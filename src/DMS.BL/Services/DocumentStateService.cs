using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class DocumentStateService : IDocumentStateService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStateTransitionRuleRepository _ruleRepository;
    private readonly IStateTransitionLogRepository _logRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly IStorageProvider? _wormProvider;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DocumentStateService> _logger;

    // States that make a document immutable
    private static readonly HashSet<DocumentState> ImmutableStates = new()
    {
        DocumentState.Record,
        DocumentState.Archived,
        DocumentState.OnHold,
        DocumentState.PendingDisposal,
        DocumentState.Quarantined
    };

    public DocumentStateService(
        IDocumentRepository documentRepository,
        IStateTransitionRuleRepository ruleRepository,
        IStateTransitionLogRepository logRepository,
        IActivityLogService activityLogService,
        IFileStorageService fileStorageService,
        ILogger<DocumentStateService> logger,
        IStorageProvider? wormProvider = null)
    {
        _documentRepository = documentRepository;
        _ruleRepository = ruleRepository;
        _logRepository = logRepository;
        _activityLogService = activityLogService;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _wormProvider = wormProvider;
    }

    public async Task<ServiceResult<DocumentDto>> TransitionAsync(Guid documentId, StateTransitionRequestDto dto, Guid userId)
    {
        if (!Enum.TryParse<DocumentState>(dto.TargetState, true, out var targetState))
            return ServiceResult<DocumentDto>.Fail($"Invalid target state: {dto.TargetState}");

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<DocumentDto>.Fail("Document not found");

        if (document.State == DocumentState.OnHold)
            return ServiceResult<DocumentDto>.Fail("Cannot manually transition a document under legal hold. Release the hold first.");

        if (document.IsCheckedOut)
            return ServiceResult<DocumentDto>.Fail("Cannot transition a checked-out document");

        var currentState = document.State;

        // Look up the transition rule
        var rule = await _ruleRepository.GetRuleAsync(currentState, targetState);
        if (rule == null)
            return ServiceResult<DocumentDto>.Fail($"Invalid state transition from {currentState} to {targetState}");

        // Validate preconditions
        if (rule.RequiresClassification && !document.ClassificationId.HasValue)
            return ServiceResult<DocumentDto>.Fail("Document must have a classification assigned before this transition");

        if (rule.RequiresRetentionPolicy && !document.RetentionPolicyId.HasValue)
            return ServiceResult<DocumentDto>.Fail("Document must have a retention policy assigned before this transition");

        // Apply the transition
        var previousState = document.State;
        document.State = targetState;
        document.StateChangedAt = DateTime.Now;
        document.StateChangedBy = userId;
        document.ModifiedBy = userId;

        if (targetState == DocumentState.Archived)
        {
            document.ArchivedAt = DateTime.Now;
            document.ArchivedBy = userId;
        }
        else if (targetState == DocumentState.Disposed)
        {
            document.DisposedAt = DateTime.Now;
            document.DisposedBy = userId;
        }

        await _documentRepository.UpdateAsync(document);

        // Copy to WORM storage when entering Record or Archived state
        if (_wormProvider != null && (targetState == DocumentState.Record || targetState == DocumentState.Archived))
        {
            if (!string.IsNullOrEmpty(document.StoragePath))
            {
                var fileStream = await _fileStorageService.GetFileAsync(document.StoragePath, document.IsEncrypted);
                if (fileStream != null)
                {
                    await _wormProvider.SaveAsync(fileStream, document.StoragePath);
                    await fileStream.DisposeAsync();
                }
            }
        }

        // Log the transition
        await _logRepository.CreateAsync(new StateTransitionLog
        {
            DocumentId = documentId,
            FromState = previousState,
            ToState = targetState,
            TransitionedBy = userId,
            TransitionedAt = DateTime.Now,
            Reason = dto.Reason,
            RuleId = rule.Id,
            IsSystemAction = false
        });

        await _activityLogService.LogActivityAsync(
            "StateTransitioned", "Document", documentId, document.Name,
            $"State changed from {previousState} to {targetState}" + (dto.Reason != null ? $": {dto.Reason}" : ""),
            userId, null, null);

        _logger.LogInformation("Document {DocumentId} transitioned from {From} to {To} by {UserId}",
            documentId, previousState, targetState, userId);

        return ServiceResult<DocumentDto>.Ok(MapToDto(document),
            $"Document transitioned from {previousState} to {targetState}");
    }

    public async Task<ServiceResult> PlaceOnHoldAsync(Guid documentId, Guid legalHoldId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.State == DocumentState.OnHold)
            return ServiceResult.Fail("Document is already on hold");

        if (document.State == DocumentState.Disposed)
            return ServiceResult.Fail("Cannot place a disposed document on hold");

        var previousState = document.State;
        document.PreviousState = previousState;
        document.State = DocumentState.OnHold;
        document.IsOnLegalHold = true;
        document.LegalHoldId = legalHoldId;
        document.LegalHoldAppliedAt = DateTime.Now;
        document.LegalHoldAppliedBy = userId;
        document.StateChangedAt = DateTime.Now;
        document.StateChangedBy = userId;
        document.ModifiedBy = userId;

        await _documentRepository.UpdateAsync(document);

        await _logRepository.CreateAsync(new StateTransitionLog
        {
            DocumentId = documentId,
            FromState = previousState,
            ToState = DocumentState.OnHold,
            TransitionedBy = userId,
            TransitionedAt = DateTime.Now,
            Reason = $"Legal hold applied (Hold ID: {legalHoldId})",
            IsSystemAction = true
        });

        await _activityLogService.LogActivityAsync(
            "LegalHoldApplied", "Document", documentId, document.Name,
            $"Placed on legal hold. Previous state: {previousState}", userId, null, null);

        return ServiceResult.Ok("Document placed on legal hold");
    }

    public async Task<ServiceResult> ReleaseFromHoldAsync(Guid documentId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.State != DocumentState.OnHold)
            return ServiceResult.Fail("Document is not on hold");

        var restoredState = document.PreviousState ?? DocumentState.Active;
        document.State = restoredState;
        document.PreviousState = null;
        document.IsOnLegalHold = false;
        document.LegalHoldId = null;
        document.LegalHoldAppliedAt = null;
        document.LegalHoldAppliedBy = null;
        document.StateChangedAt = DateTime.Now;
        document.StateChangedBy = userId;
        document.ModifiedBy = userId;

        await _documentRepository.UpdateAsync(document);

        await _logRepository.CreateAsync(new StateTransitionLog
        {
            DocumentId = documentId,
            FromState = DocumentState.OnHold,
            ToState = restoredState,
            TransitionedBy = userId,
            TransitionedAt = DateTime.Now,
            Reason = $"Legal hold released. Restored to {restoredState}",
            IsSystemAction = true
        });

        await _activityLogService.LogActivityAsync(
            "LegalHoldReleased", "Document", documentId, document.Name,
            $"Legal hold released. State restored to {restoredState}", userId, null, null);

        return ServiceResult.Ok($"Document released from hold. State restored to {restoredState}");
    }

    public async Task<ServiceResult> InitiatePendingDisposalAsync(Guid documentId, Guid userId, string reason)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.IsOnLegalHold)
            return ServiceResult.Fail("Cannot initiate disposal for a document under legal hold");

        var previousState = document.State;
        document.State = DocumentState.PendingDisposal;
        document.StateChangedAt = DateTime.Now;
        document.StateChangedBy = userId;
        document.ModifiedBy = userId;

        await _documentRepository.UpdateAsync(document);

        await _logRepository.CreateAsync(new StateTransitionLog
        {
            DocumentId = documentId,
            FromState = previousState,
            ToState = DocumentState.PendingDisposal,
            TransitionedBy = userId,
            TransitionedAt = DateTime.Now,
            Reason = reason,
            IsSystemAction = true
        });

        await _activityLogService.LogActivityAsync(
            "PendingDisposal", "Document", documentId, document.Name,
            $"Disposal initiated: {reason}", userId, null, null);

        return ServiceResult.Ok("Document moved to pending disposal");
    }

    public async Task<ServiceResult<List<AllowedTransitionDto>>> GetAllowedTransitionsAsync(
        Guid documentId, Guid userId, IEnumerable<string> userRoles)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<List<AllowedTransitionDto>>.Fail("Document not found");

        var rules = await _ruleRepository.GetRulesFromStateAsync(document.State);
        var roleSet = new HashSet<string>(userRoles, StringComparer.OrdinalIgnoreCase);
        var isAdmin = roleSet.Contains("Admin") || roleSet.Contains("Administrator");

        var allowed = new List<AllowedTransitionDto>();
        foreach (var rule in rules)
        {
            // Check role requirement
            if (!string.IsNullOrEmpty(rule.RequiredRole) && !isAdmin && !roleSet.Contains(rule.RequiredRole))
                continue;

            // System-only transitions (OnHold) are not manually selectable
            if (rule.RequiredRole == "System")
                continue;

            allowed.Add(new AllowedTransitionDto
            {
                FromState = rule.FromState.ToString(),
                ToState = rule.ToState.ToString(),
                Description = rule.Description,
                RequiresApproval = rule.RequiresApproval,
                RequiresClassification = rule.RequiresClassification,
                RequiresRetentionPolicy = rule.RequiresRetentionPolicy
            });
        }

        return ServiceResult<List<AllowedTransitionDto>>.Ok(allowed);
    }

    public async Task<ServiceResult<List<StateTransitionLogDto>>> GetTransitionHistoryAsync(Guid documentId)
    {
        var logs = await _logRepository.GetByDocumentIdAsync(documentId);
        var dtos = logs.Select(l => new StateTransitionLogDto
        {
            Id = l.Id,
            DocumentId = l.DocumentId,
            FromState = l.FromState.ToString(),
            ToState = l.ToState.ToString(),
            TransitionedBy = l.TransitionedBy,
            TransitionedAt = l.TransitionedAt,
            Reason = l.Reason,
            IsSystemAction = l.IsSystemAction
        }).ToList();

        return ServiceResult<List<StateTransitionLogDto>>.Ok(dtos);
    }

    public bool IsImmutable(DocumentState state) => ImmutableStates.Contains(state);

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            FolderId = document.FolderId,
            Name = document.Name,
            Description = document.Description,
            Extension = document.Extension,
            ContentType = document.ContentType,
            Size = document.Size,
            CurrentVersion = document.CurrentVersion,
            CurrentMajorVersion = document.CurrentMajorVersion,
            CurrentMinorVersion = document.CurrentMinorVersion,
            IsCheckedOut = document.IsCheckedOut,
            CheckedOutBy = document.CheckedOutBy,
            CheckedOutAt = document.CheckedOutAt,
            ClassificationId = document.ClassificationId,
            ImportanceId = document.ImportanceId,
            DocumentTypeId = document.DocumentTypeId,
            ContentTypeId = document.ContentTypeId,
            CreatedBy = document.CreatedBy,
            CreatedAt = document.CreatedAt,
            ModifiedAt = document.ModifiedAt,
            ExpiryDate = document.ExpiryDate,
            PrivacyLevelId = document.PrivacyLevelId,
            State = document.State.ToString(),
            IsEncrypted = document.IsEncrypted,
            IsPdfACompliant = document.IsPdfACompliant
        };
    }
}

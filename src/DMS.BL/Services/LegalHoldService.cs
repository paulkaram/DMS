using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// ISO 15489 compliant legal hold management service.
/// </summary>
public class LegalHoldService : ILegalHoldService
{
    private readonly ILegalHoldRepository _holdRepository;
    private readonly ILegalHoldDocumentRepository _holdDocumentRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<LegalHoldService> _logger;

    public LegalHoldService(
        ILegalHoldRepository holdRepository,
        ILegalHoldDocumentRepository holdDocumentRepository,
        IDocumentRepository documentRepository,
        IActivityLogService activityLogService,
        ILogger<LegalHoldService> logger)
    {
        _holdRepository = holdRepository;
        _holdDocumentRepository = holdDocumentRepository;
        _documentRepository = documentRepository;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<ServiceResult<LegalHoldDto>> CreateHoldAsync(CreateLegalHoldDto dto, Guid userId)
    {
        var hold = new LegalHold
        {
            HoldNumber = await GenerateHoldNumberAsync(),
            Name = dto.Name,
            Description = dto.Description,
            CaseReference = dto.CaseReference,
            RequestedBy = dto.RequestedBy,
            RequestedAt = dto.RequestedAt,
            Status = LegalHoldStatus.Active,
            EffectiveFrom = dto.EffectiveFrom ?? DateTime.Now,
            EffectiveUntil = dto.EffectiveUntil,
            AppliedBy = userId,
            AppliedAt = DateTime.Now,
            Notes = dto.Notes,
            IsActive = true
        };

        var id = await _holdRepository.CreateAsync(hold);
        hold.Id = id;

        // Add initial documents if provided
        if (dto.InitialDocumentIds?.Any() == true)
        {
            await AddDocumentsToHoldAsync(id, dto.InitialDocumentIds, userId, "Added during hold creation");
        }

        await _activityLogService.LogActivityAsync(
            "LegalHoldCreated", "LegalHold", id, hold.Name,
            $"Legal hold created: {hold.HoldNumber}", userId, null, null);

        _logger.LogInformation("Legal hold created: {HoldNumber} by user {UserId}", hold.HoldNumber, userId);

        return ServiceResult<LegalHoldDto>.Ok(MapToDto(hold), "Legal hold created successfully");
    }

    public async Task<ServiceResult<LegalHoldDto>> GetByIdAsync(Guid id)
    {
        var hold = await _holdRepository.GetByIdAsync(id);
        if (hold == null)
            return ServiceResult<LegalHoldDto>.Fail("Legal hold not found");

        var dto = MapToDto(hold);
        var docs = await _holdDocumentRepository.GetByHoldIdAsync(id);
        dto.DocumentCount = docs.Count(d => d.ReleasedAt == null);

        return ServiceResult<LegalHoldDto>.Ok(dto);
    }

    public async Task<ServiceResult<List<LegalHoldDto>>> GetActiveHoldsAsync()
    {
        var holds = await _holdRepository.GetActiveAsync();
        var dtos = new List<LegalHoldDto>();

        foreach (var hold in holds)
        {
            var dto = MapToDto(hold);
            var docs = await _holdDocumentRepository.GetByHoldIdAsync(hold.Id);
            dto.DocumentCount = docs.Count(d => d.ReleasedAt == null);
            dtos.Add(dto);
        }

        return ServiceResult<List<LegalHoldDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<LegalHoldDto>>> GetAllHoldsAsync()
    {
        var holds = await _holdRepository.GetAllAsync();
        var dtos = new List<LegalHoldDto>();

        foreach (var hold in holds)
        {
            var dto = MapToDto(hold);
            var docs = await _holdDocumentRepository.GetByHoldIdAsync(hold.Id);
            dto.DocumentCount = docs.Count(d => d.ReleasedAt == null);
            dtos.Add(dto);
        }

        return ServiceResult<List<LegalHoldDto>>.Ok(dtos);
    }

    public async Task<ServiceResult> AddDocumentsToHoldAsync(Guid holdId, List<Guid> documentIds, Guid userId, string? notes = null)
    {
        var hold = await _holdRepository.GetByIdAsync(holdId);
        if (hold == null)
            return ServiceResult.Fail("Legal hold not found");

        if (hold.Status != LegalHoldStatus.Active)
            return ServiceResult.Fail("Cannot add documents to inactive hold");

        var addedCount = 0;
        foreach (var documentId in documentIds)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null) continue;

            // Check if already on this hold
            var existing = await _holdDocumentRepository.GetByDocumentIdAsync(documentId);
            if (existing.Any(e => e.LegalHoldId == holdId && e.ReleasedAt == null))
                continue;

            var holdDoc = new LegalHoldDocument
            {
                LegalHoldId = holdId,
                DocumentId = documentId,
                AddedAt = DateTime.Now,
                AddedBy = userId,
                Notes = notes
            };

            await _holdDocumentRepository.CreateAsync(holdDoc);

            // Update document to indicate it's on hold
            document.IsOnLegalHold = true;
            document.LegalHoldId = holdId;
            document.LegalHoldAppliedAt = DateTime.Now;
            document.LegalHoldAppliedBy = userId;
            await _documentRepository.UpdateAsync(document);

            addedCount++;

            await _activityLogService.LogActivityAsync(
                "LegalHoldApplied", "Document", documentId, document.Name,
                $"Added to legal hold: {hold.HoldNumber}", userId, null, null);
        }

        _logger.LogInformation("Added {Count} documents to legal hold {HoldNumber}", addedCount, hold.HoldNumber);

        return ServiceResult.Ok($"{addedCount} document(s) added to legal hold");
    }

    public async Task<ServiceResult> RemoveDocumentFromHoldAsync(Guid holdId, Guid documentId, Guid userId)
    {
        var hold = await _holdRepository.GetByIdAsync(holdId);
        if (hold == null)
            return ServiceResult.Fail("Legal hold not found");

        var holdDocs = await _holdDocumentRepository.GetByDocumentIdAsync(documentId);
        var holdDoc = holdDocs.FirstOrDefault(h => h.LegalHoldId == holdId && h.ReleasedAt == null);

        if (holdDoc == null)
            return ServiceResult.Fail("Document is not under this legal hold");

        holdDoc.ReleasedAt = DateTime.Now;
        holdDoc.ReleasedBy = userId;
        await _holdDocumentRepository.UpdateAsync(holdDoc);

        // Check if document is still on any other active holds
        var otherActiveHolds = await _holdDocumentRepository.GetActiveByDocumentIdAsync(documentId);
        if (!otherActiveHolds.Any())
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                document.IsOnLegalHold = false;
                document.LegalHoldId = null;
                await _documentRepository.UpdateAsync(document);
            }
        }

        await _activityLogService.LogActivityAsync(
            "LegalHoldReleased", "Document", documentId, holdDoc.DocumentName ?? "Unknown",
            $"Released from legal hold: {hold.HoldNumber}", userId, null, null);

        return ServiceResult.Ok("Document released from legal hold");
    }

    public async Task<ServiceResult<List<LegalHoldDocumentDto>>> GetHoldDocumentsAsync(Guid holdId)
    {
        var holdDocs = await _holdDocumentRepository.GetByHoldIdAsync(holdId);
        var dtos = holdDocs.Select(h => new LegalHoldDocumentDto
        {
            Id = h.Id,
            LegalHoldId = h.LegalHoldId,
            HoldName = h.HoldName ?? string.Empty,
            DocumentId = h.DocumentId,
            DocumentName = h.DocumentName ?? string.Empty,
            AddedAt = h.AddedAt,
            AddedBy = h.AddedBy,
            ReleasedAt = h.ReleasedAt,
            ReleasedBy = h.ReleasedBy,
            Notes = h.Notes
        }).ToList();

        return ServiceResult<List<LegalHoldDocumentDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<LegalHoldDto>>> GetDocumentHoldsAsync(Guid documentId)
    {
        var holdDocs = await _holdDocumentRepository.GetByDocumentIdAsync(documentId);
        var dtos = new List<LegalHoldDto>();

        foreach (var holdDoc in holdDocs)
        {
            var hold = await _holdRepository.GetByIdAsync(holdDoc.LegalHoldId);
            if (hold != null)
            {
                dtos.Add(MapToDto(hold));
            }
        }

        return ServiceResult<List<LegalHoldDto>>.Ok(dtos);
    }

    public async Task<bool> IsDocumentOnHoldAsync(Guid documentId)
    {
        return await _holdDocumentRepository.IsDocumentOnHoldAsync(documentId);
    }

    public async Task<ServiceResult> ReleaseHoldAsync(Guid holdId, Guid userId, string reason)
    {
        var hold = await _holdRepository.GetByIdAsync(holdId);
        if (hold == null)
            return ServiceResult.Fail("Legal hold not found");

        if (hold.Status != LegalHoldStatus.Active)
            return ServiceResult.Fail("Hold is not active");

        hold.Status = LegalHoldStatus.Released;
        hold.ReleasedBy = userId;
        hold.ReleasedAt = DateTime.Now;
        hold.ReleaseReason = reason;

        await _holdRepository.UpdateAsync(hold);

        // Release all documents from this hold
        var holdDocs = await _holdDocumentRepository.GetByHoldIdAsync(holdId);
        foreach (var holdDoc in holdDocs.Where(h => h.ReleasedAt == null))
        {
            holdDoc.ReleasedAt = DateTime.Now;
            holdDoc.ReleasedBy = userId;
            await _holdDocumentRepository.UpdateAsync(holdDoc);

            // Check if document is on any other active holds
            var otherActiveHolds = await _holdDocumentRepository.GetActiveByDocumentIdAsync(holdDoc.DocumentId);
            if (!otherActiveHolds.Any())
            {
                var document = await _documentRepository.GetByIdAsync(holdDoc.DocumentId);
                if (document != null)
                {
                    document.IsOnLegalHold = false;
                    document.LegalHoldId = null;
                    await _documentRepository.UpdateAsync(document);
                }
            }
        }

        await _activityLogService.LogActivityAsync(
            "LegalHoldReleased", "LegalHold", holdId, hold.Name,
            $"Legal hold released: {reason}", userId, null, null);

        _logger.LogInformation("Legal hold {HoldNumber} released by user {UserId}", hold.HoldNumber, userId);

        return ServiceResult.Ok("Legal hold released successfully");
    }

    public async Task<ServiceResult<LegalHoldDto>> UpdateHoldAsync(Guid id, UpdateLegalHoldDto dto, Guid userId)
    {
        var hold = await _holdRepository.GetByIdAsync(id);
        if (hold == null)
            return ServiceResult<LegalHoldDto>.Fail("Legal hold not found");

        if (dto.Name != null) hold.Name = dto.Name;
        if (dto.Description != null) hold.Description = dto.Description;
        if (dto.CaseReference != null) hold.CaseReference = dto.CaseReference;
        if (dto.RequestedBy != null) hold.RequestedBy = dto.RequestedBy;
        if (dto.EffectiveUntil.HasValue) hold.EffectiveUntil = dto.EffectiveUntil;
        if (dto.Notes != null) hold.Notes = dto.Notes;

        await _holdRepository.UpdateAsync(hold);

        await _activityLogService.LogActivityAsync(
            "LegalHoldUpdated", "LegalHold", id, hold.Name,
            "Legal hold updated", userId, null, null);

        return ServiceResult<LegalHoldDto>.Ok(MapToDto(hold), "Legal hold updated successfully");
    }

    private async Task<string> GenerateHoldNumberAsync()
    {
        // Generate format: LH-YYYYMMDD-XXXX
        var holds = await _holdRepository.GetAllAsync();
        var today = DateTime.Now.ToString("yyyyMMdd");
        var todayHolds = holds.Count(h => h.HoldNumber.StartsWith($"LH-{today}"));
        return $"LH-{today}-{(todayHolds + 1):D4}";
    }

    private static LegalHoldDto MapToDto(LegalHold hold)
    {
        return new LegalHoldDto
        {
            Id = hold.Id,
            HoldNumber = hold.HoldNumber,
            Name = hold.Name,
            Description = hold.Description,
            CaseReference = hold.CaseReference,
            RequestedBy = hold.RequestedBy,
            RequestedAt = hold.RequestedAt,
            Status = hold.Status,
            EffectiveFrom = hold.EffectiveFrom,
            EffectiveUntil = hold.EffectiveUntil,
            AppliedBy = hold.AppliedBy,
            AppliedAt = hold.AppliedAt,
            ReleasedBy = hold.ReleasedBy,
            ReleasedAt = hold.ReleasedAt,
            ReleaseReason = hold.ReleaseReason,
            Notes = hold.Notes,
            CreatedAt = hold.CreatedAt
        };
    }
}

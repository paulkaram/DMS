using System.Text.Json;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IDocumentWorkingCopyRepository _workingCopyRepository;
    private readonly IDocumentVersionMetadataRepository _versionMetadataRepository;
    private readonly IContentTypeDefinitionRepository _contentTypeRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityLogService _activityLogService;
    private readonly IRecycleBinRepository _recycleBinRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IFileValidationService _fileValidationService;
    private readonly IDocumentPasswordRepository _passwordRepository;
    private readonly IDocumentShortcutRepository _shortcutRepository;

    // JSON options for consistent serialization with camelCase
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository versionRepository,
        IDocumentWorkingCopyRepository workingCopyRepository,
        IDocumentVersionMetadataRepository versionMetadataRepository,
        IContentTypeDefinitionRepository contentTypeRepository,
        IFileStorageService fileStorageService,
        IActivityLogService activityLogService,
        IRecycleBinRepository recycleBinRepository,
        IFolderRepository folderRepository,
        IFileValidationService fileValidationService,
        IDocumentPasswordRepository passwordRepository,
        IDocumentShortcutRepository shortcutRepository)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _workingCopyRepository = workingCopyRepository;
        _versionMetadataRepository = versionMetadataRepository;
        _contentTypeRepository = contentTypeRepository;
        _fileStorageService = fileStorageService;
        _activityLogService = activityLogService;
        _recycleBinRepository = recycleBinRepository;
        _folderRepository = folderRepository;
        _fileValidationService = fileValidationService;
        _passwordRepository = passwordRepository;
        _shortcutRepository = shortcutRepository;
    }

    #region Basic CRUD Operations

    public async Task<ServiceResult<DocumentDto>> GetByIdAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<DocumentDto>.Fail("Document not found");

        var dto = MapToDto(document);
        dto.HasPassword = await _passwordRepository.HasPasswordAsync(id);
        return ServiceResult<DocumentDto>.Ok(dto);
    }

    public async Task<ServiceResult<List<DocumentDto>>> GetByFolderIdAsync(Guid folderId)
    {
        var documents = await _documentRepository.GetByFolderIdWithNamesAsync(folderId);
        var dtos = documents.Select(MapToDtoWithNames).ToList();
        await EnrichWithPasswordStatusAsync(dtos);
        return ServiceResult<List<DocumentDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<DocumentDto>>> SearchAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        var documents = await _documentRepository.SearchWithNamesAsync(name, folderId, classificationId, documentTypeId);
        var dtos = documents.Select(MapToDtoWithNames).ToList();
        await EnrichWithPasswordStatusAsync(dtos);
        return ServiceResult<List<DocumentDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<DocumentDto>>> GetCheckedOutByUserAsync(Guid userId)
    {
        var documents = await _documentRepository.GetCheckedOutByUserWithNamesAsync(userId);
        var dtos = documents.Select(MapToDtoWithNames).ToList();
        await EnrichWithPasswordStatusAsync(dtos);
        return ServiceResult<List<DocumentDto>>.Ok(dtos);
    }

    private async Task EnrichWithPasswordStatusAsync(List<DocumentDto> documents)
    {
        if (documents.Count == 0) return;

        var documentIds = documents.Select(d => d.Id).ToList();
        var passwordStatuses = await _passwordRepository.GetPasswordStatusBulkAsync(documentIds);

        foreach (var doc in documents)
        {
            doc.HasPassword = passwordStatuses.TryGetValue(doc.Id, out var hasPassword) && hasPassword;
        }
    }

    public async Task<ServiceResult<List<DocumentDto>>> GetCreatedByUserAsync(Guid userId, int take = 50)
    {
        var documents = await _documentRepository.GetCreatedByUserWithNamesAsync(userId, take);
        var dtos = documents.Select(MapToDtoWithNames).ToList();
        await EnrichWithPasswordStatusAsync(dtos);
        return ServiceResult<List<DocumentDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<DocumentVersionDto>>> GetVersionsAsync(Guid documentId)
    {
        var versions = await _versionRepository.GetByDocumentIdAsync(documentId);
        return ServiceResult<List<DocumentVersionDto>>.Ok(versions.Select(MapVersionToDto).ToList());
    }

    public async Task<ServiceResult<DocumentDto>> CreateAsync(CreateDocumentDto dto, Stream fileStream, string fileName, string contentType, Guid userId)
    {
        // Validate file before processing
        var validationResult = await _fileValidationService.ValidateFileAsync(fileStream, fileName, contentType);
        if (!validationResult.IsValid)
            return ServiceResult<DocumentDto>.Fail(validationResult.Error!);

        // Use validated content type from database
        var validatedContentType = validationResult.ValidatedMimeType ?? contentType;

        var extension = Path.GetExtension(fileName);
        var document = new Document
        {
            FolderId = dto.FolderId,
            Name = dto.Name,
            Description = dto.Description,
            Extension = extension,
            ContentType = validatedContentType,
            Size = fileStream.Length,
            CurrentVersion = 1,
            CurrentMajorVersion = 1,
            CurrentMinorVersion = 0,
            ClassificationId = dto.ClassificationId,
            ImportanceId = dto.ImportanceId,
            DocumentTypeId = dto.DocumentTypeId,
            CreatedBy = userId,
            IsActive = true,
            IsOriginalRecord = true,
            ContentCategory = "Original"
        };

        var id = await _documentRepository.CreateAsync(document);
        document.Id = id;

        // Save file with integrity hash (ISO 27001 compliance)
        var storageResult = await _fileStorageService.SaveFileWithHashAsync(fileStream, fileName, id, 1);
        document.StoragePath = storageResult.StoragePath;
        document.IntegrityHash = storageResult.IntegrityHash;
        document.HashAlgorithm = storageResult.HashAlgorithm;
        document.IntegrityVerifiedAt = DateTime.UtcNow;
        document.Size = storageResult.Size;

        // Create first version (1.0 Major)
        var version = new DocumentVersion
        {
            DocumentId = id,
            VersionNumber = 1,
            StoragePath = storageResult.StoragePath,
            Size = storageResult.Size,
            Comment = "Initial version",
            CreatedBy = userId,
            IntegrityHash = storageResult.IntegrityHash,
            HashAlgorithm = storageResult.HashAlgorithm,
            IntegrityVerifiedAt = DateTime.UtcNow,
            ContentType = validatedContentType,
            OriginalFileName = fileName,
            IsOriginalRecord = true,
            ContentCategory = "Original",
            VersionType = "Major",
            MajorVersion = 1,
            MinorVersion = 0,
            VersionLabel = "1.0",
            IsContentChanged = true,
            IsMetadataChanged = false,
            ChangeDescription = "Initial upload"
        };
        var versionId = await _versionRepository.CreateAsync(version);

        // Update document with current version reference
        document.CurrentVersionId = versionId;
        await _documentRepository.UpdateAsync(document);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Created, "Document", id, dto.Name, null, userId, null, null);

        return ServiceResult<DocumentDto>.Ok(MapToDto(document), "Document created successfully");
    }

    public async Task<ServiceResult<DocumentDto>> UpdateMetadataAsync(Guid id, UpdateDocumentDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<DocumentDto>.Fail("Document not found");

        // ISO 15489: Enforce checkout for metadata editing
        if (!document.IsCheckedOut)
            return ServiceResult<DocumentDto>.Fail("Document must be checked out before editing. Please check out the document first.");

        if (document.CheckedOutBy != userId)
            return ServiceResult<DocumentDto>.Fail("Document is checked out by another user");

        // Save changes to working copy, not directly to document
        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(id);
        if (workingCopy == null)
            return ServiceResult<DocumentDto>.Fail("Working copy not found. Document may not be properly checked out.");

        workingCopy.DraftName = dto.Name;
        workingCopy.DraftDescription = dto.Description;
        workingCopy.DraftClassificationId = dto.ClassificationId;
        workingCopy.DraftImportanceId = dto.ImportanceId;
        workingCopy.DraftDocumentTypeId = dto.DocumentTypeId;
        workingCopy.LastModifiedAt = DateTime.UtcNow;

        await _workingCopyRepository.UpdateAsync(workingCopy);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Updated, "Document", id, dto.Name, "Draft metadata saved", userId, null, null);

        return ServiceResult<DocumentDto>.Ok(MapToDto(document), "Draft changes saved. Check in the document to create a new version.");
    }

    public async Task<ServiceResult<DocumentDto>> UpdateContentAsync(Guid id, Stream fileStream, string fileName, string contentType, Guid userId)
    {
        // Validate file before processing
        var validationResult = await _fileValidationService.ValidateFileAsync(fileStream, fileName, contentType);
        if (!validationResult.IsValid)
            return ServiceResult<DocumentDto>.Fail(validationResult.Error!);

        var validatedContentType = validationResult.ValidatedMimeType ?? contentType;

        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<DocumentDto>.Fail("Document not found");

        // ISO 15489: Enforce checkout for content replacement
        if (!document.IsCheckedOut)
            return ServiceResult<DocumentDto>.Fail("Document must be checked out before replacing content. Please check out the document first.");

        if (document.CheckedOutBy != userId)
            return ServiceResult<DocumentDto>.Fail("Document is checked out by another user");

        // Save draft content to working copy
        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(id);
        if (workingCopy == null)
            return ServiceResult<DocumentDto>.Fail("Working copy not found");

        // Save to draft storage (not final location)
        var draftStorageResult = await _fileStorageService.SaveFileWithHashAsync(
            fileStream, fileName, id, document.CurrentVersion + 1);

        workingCopy.DraftStoragePath = draftStorageResult.StoragePath;
        workingCopy.DraftSize = draftStorageResult.Size;
        workingCopy.DraftContentType = validatedContentType;
        workingCopy.DraftOriginalFileName = fileName;
        workingCopy.DraftIntegrityHash = draftStorageResult.IntegrityHash;
        workingCopy.LastModifiedAt = DateTime.UtcNow;

        await _workingCopyRepository.UpdateAsync(workingCopy);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Updated, "Document", id, document.Name, "Draft content saved", userId, null, null);

        var message = "Draft content saved. Check in the document to create a new version.";
        if (!string.IsNullOrEmpty(validationResult.Warning))
            message = $"{validationResult.Warning} {message}";

        return ServiceResult<DocumentDto>.Ok(MapToDto(document), message);
    }

    public async Task<ServiceResult<Stream>> DownloadAsync(Guid id, int? version = null)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<Stream>.Fail("Document not found");

        string? storagePath;
        if (version.HasValue)
        {
            var versions = await _versionRepository.GetByDocumentIdAsync(id);
            var targetVersion = versions.FirstOrDefault(v => v.VersionNumber == version.Value);
            if (targetVersion == null)
                return ServiceResult<Stream>.Fail("Version not found");
            storagePath = targetVersion.StoragePath;
        }
        else
        {
            storagePath = document.StoragePath;
        }

        if (string.IsNullOrEmpty(storagePath))
            return ServiceResult<Stream>.Fail("File not found");

        var stream = await _fileStorageService.GetFileAsync(storagePath);
        if (stream == null)
            return ServiceResult<Stream>.Fail("File not found");

        return ServiceResult<Stream>.Ok(stream);
    }

    public async Task<ServiceResult> MoveAsync(Guid id, MoveDocumentDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.IsCheckedOut)
            return ServiceResult.Fail("Cannot move a checked out document");

        document.FolderId = dto.NewFolderId;
        document.ModifiedBy = userId;

        await _documentRepository.UpdateAsync(document);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Moved, "Document", id, document.Name, null, userId, null, null);

        return ServiceResult.Ok("Document moved successfully");
    }

    public async Task<ServiceResult<DocumentDto>> CopyAsync(Guid id, CopyDocumentDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<DocumentDto>.Fail("Document not found");

        using var sourceStream = await _fileStorageService.GetFileAsync(document.StoragePath!);
        if (sourceStream == null)
            return ServiceResult<DocumentDto>.Fail($"Source file not found at: {document.StoragePath}");

        var newDocument = new Document
        {
            FolderId = dto.TargetFolderId,
            Name = dto.NewName ?? document.Name,
            Description = document.Description,
            Extension = document.Extension,
            ContentType = document.ContentType,
            Size = document.Size,
            CurrentVersion = 1,
            CurrentMajorVersion = 1,
            CurrentMinorVersion = 0,
            ClassificationId = document.ClassificationId,
            ImportanceId = document.ImportanceId,
            DocumentTypeId = document.DocumentTypeId,
            CreatedBy = userId,
            IsActive = true,
            IsOriginalRecord = false,
            SourceDocumentId = document.Id,
            ContentCategory = "Original"
        };

        var newId = await _documentRepository.CreateAsync(newDocument);
        newDocument.Id = newId;

        var storageResult = await _fileStorageService.SaveFileWithHashAsync(
            sourceStream, newDocument.Name + newDocument.Extension, newId, 1);
        newDocument.StoragePath = storageResult.StoragePath;
        newDocument.IntegrityHash = storageResult.IntegrityHash;
        newDocument.HashAlgorithm = storageResult.HashAlgorithm;
        newDocument.IntegrityVerifiedAt = DateTime.UtcNow;

        var version = new DocumentVersion
        {
            DocumentId = newId,
            VersionNumber = 1,
            StoragePath = storageResult.StoragePath,
            Size = newDocument.Size,
            Comment = $"Copied from {document.Name}",
            CreatedBy = userId,
            IntegrityHash = storageResult.IntegrityHash,
            HashAlgorithm = storageResult.HashAlgorithm,
            VersionType = "Major",
            MajorVersion = 1,
            MinorVersion = 0,
            VersionLabel = "1.0",
            IsContentChanged = true,
            ChangeDescription = $"Copied from document {document.Id}"
        };
        var versionId = await _versionRepository.CreateAsync(version);
        newDocument.CurrentVersionId = versionId;
        await _documentRepository.UpdateAsync(newDocument);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Copied, "Document", newId, newDocument.Name, $"Copied from {document.Name}", userId, null, null);

        return ServiceResult<DocumentDto>.Ok(MapToDto(newDocument), "Document copied successfully");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.IsCheckedOut)
            return ServiceResult.Fail("Cannot delete a checked out document");

        if (document.IsOnLegalHold)
            return ServiceResult.Fail("Cannot delete a document under legal hold");

        string originalPath = await BuildDocumentPathAsync(document);

        var recycleBinItem = new RecycleBinItem
        {
            NodeType = 3,
            NodeId = document.Id,
            NodeName = document.Name + (document.Extension ?? ""),
            OriginalPath = originalPath,
            OriginalParentId = document.FolderId,
            DeletedBy = userId,
            Metadata = JsonSerializer.Serialize(new
            {
                document.Extension,
                document.ContentType,
                document.Size,
                document.CurrentVersion,
                document.CurrentMajorVersion,
                document.CurrentMinorVersion
            }, _jsonOptions)
        };
        await _recycleBinRepository.AddAsync(recycleBinItem);

        await _documentRepository.DeleteAsync(id);

        // Clean up all shortcuts pointing to this document
        await _shortcutRepository.DeleteAllByDocumentIdAsync(id);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Deleted, "Document", id, document.Name, null, userId, null, null);

        return ServiceResult.Ok("Document moved to recycle bin");
    }

    #endregion

    #region ISO 15489 Checkout System

    public async Task<ServiceResult> CheckOutAsync(Guid id, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (document.IsCheckedOut)
            return ServiceResult.Fail($"Document is already checked out");

        if (document.IsOnLegalHold)
            return ServiceResult.Fail("Cannot check out a document under legal hold");

        // Set checkout flags on document
        var success = await _documentRepository.CheckOutAsync(id, userId);
        if (!success)
            return ServiceResult.Fail("Failed to check out document");

        // Create working copy for draft storage
        var workingCopy = new DocumentWorkingCopy
        {
            DocumentId = id,
            CheckedOutBy = userId,
            DraftName = document.Name,
            DraftDescription = document.Description,
            DraftClassificationId = document.ClassificationId,
            DraftImportanceId = document.ImportanceId,
            DraftDocumentTypeId = document.DocumentTypeId,
            AutoSaveEnabled = true
        };
        await _workingCopyRepository.CreateAsync(workingCopy);

        await _activityLogService.LogActivityAsync(
            ActivityActions.CheckedOut, "Document", id, document.Name,
            $"Checked out at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            userId, null, null);

        return ServiceResult.Ok("Document checked out successfully. You can now edit metadata or upload new content.");
    }

    public async Task<ServiceResult<DocumentVersionDto>> CheckInAsync(Guid id, Stream? fileStream, string? fileName,
        string? contentType, CheckInDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult<DocumentVersionDto>.Fail("Document not found");

        if (!document.IsCheckedOut || document.CheckedOutBy != userId)
            return ServiceResult<DocumentVersionDto>.Fail("Document is not checked out by you");

        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(id);
        if (workingCopy == null)
            return ServiceResult<DocumentVersionDto>.Fail("Working copy not found");

        // Get current version for linking
        var currentVersion = await _versionRepository.GetLatestVersionAsync(id);

        // Calculate new version numbers based on check-in type
        int newMajorVersion = document.CurrentMajorVersion;
        int newMinorVersion = document.CurrentMinorVersion;
        string versionType;

        switch (dto.CheckInType)
        {
            case CheckInType.Major:
                newMajorVersion++;
                newMinorVersion = 0;
                versionType = "Major";
                break;
            case CheckInType.Minor:
                newMinorVersion++;
                versionType = "Minor";
                break;
            case CheckInType.Overwrite:
                // Keep current version numbers
                versionType = currentVersion?.VersionType ?? "Minor";
                break;
            default:
                newMinorVersion++;
                versionType = "Minor";
                break;
        }

        var newVersionNumber = document.CurrentVersion + 1;
        var versionLabel = $"{newMajorVersion}.{newMinorVersion}";

        // Determine what changed
        bool contentChanged = fileStream != null || !string.IsNullOrEmpty(workingCopy.DraftStoragePath);
        bool metadataChanged = HasMetadataChanges(document, workingCopy);

        // Handle file content
        string? storagePath = document.StoragePath;
        long size = document.Size;
        string? integrityHash = document.IntegrityHash;
        string? hashAlgorithm = document.HashAlgorithm;
        string? finalContentType = document.ContentType;
        string? originalFileName = null;

        if (fileStream != null && !string.IsNullOrEmpty(fileName))
        {
            // Validate file before processing
            var validationResult = await _fileValidationService.ValidateFileAsync(fileStream, fileName, contentType ?? "application/octet-stream");
            if (!validationResult.IsValid)
                return ServiceResult<DocumentVersionDto>.Fail(validationResult.Error!);

            var validatedContentType = validationResult.ValidatedMimeType ?? contentType;

            // New file uploaded during check-in
            var storageResult = await _fileStorageService.SaveFileWithHashAsync(fileStream, fileName, id, newVersionNumber);
            storagePath = storageResult.StoragePath;
            size = storageResult.Size;
            integrityHash = storageResult.IntegrityHash;
            hashAlgorithm = storageResult.HashAlgorithm;
            finalContentType = validatedContentType;
            originalFileName = fileName;
            contentChanged = true;
        }
        else if (!string.IsNullOrEmpty(workingCopy.DraftStoragePath))
        {
            // Use draft file from working copy
            storagePath = workingCopy.DraftStoragePath;
            size = workingCopy.DraftSize ?? document.Size;
            integrityHash = workingCopy.DraftIntegrityHash;
            hashAlgorithm = "SHA256";
            finalContentType = workingCopy.DraftContentType ?? document.ContentType;
            originalFileName = workingCopy.DraftOriginalFileName;
            contentChanged = true;
        }

        // Create new version record
        var newVersion = new DocumentVersion
        {
            DocumentId = id,
            VersionNumber = newVersionNumber,
            StoragePath = storagePath,
            Size = size,
            Comment = dto.Comment,
            CreatedBy = userId,
            IntegrityHash = integrityHash,
            HashAlgorithm = hashAlgorithm,
            IntegrityVerifiedAt = DateTime.UtcNow,
            ContentType = finalContentType,
            OriginalFileName = originalFileName ?? document.Name + document.Extension,
            IsOriginalRecord = true,
            ContentCategory = "Original",
            VersionType = versionType,
            MajorVersion = newMajorVersion,
            MinorVersion = newMinorVersion,
            VersionLabel = versionLabel,
            IsContentChanged = contentChanged,
            IsMetadataChanged = metadataChanged,
            PreviousVersionId = currentVersion?.Id,
            ChangeDescription = dto.ChangeDescription ?? dto.Comment
        };
        var newVersionId = await _versionRepository.CreateAsync(newVersion);

        // Apply working copy changes to document (if not overwrite)
        if (dto.CheckInType != CheckInType.Overwrite)
        {
            document.CurrentVersion = newVersionNumber;
            document.CurrentMajorVersion = newMajorVersion;
            document.CurrentMinorVersion = newMinorVersion;
        }

        document.CurrentVersionId = newVersionId;
        document.StoragePath = storagePath;
        document.Size = size;
        document.IntegrityHash = integrityHash;
        document.HashAlgorithm = hashAlgorithm;
        document.IntegrityVerifiedAt = DateTime.UtcNow;
        document.ContentType = finalContentType;
        document.ModifiedBy = userId;

        // Update extension when a new file is uploaded with a different type
        if (contentChanged && !string.IsNullOrEmpty(originalFileName))
        {
            var newExtension = Path.GetExtension(originalFileName);
            if (!string.IsNullOrEmpty(newExtension))
            {
                document.Extension = newExtension;
            }
        }

        // Apply metadata changes from working copy
        if (!string.IsNullOrEmpty(workingCopy.DraftName))
            document.Name = workingCopy.DraftName;
        if (workingCopy.DraftDescription != null)
            document.Description = workingCopy.DraftDescription;
        if (workingCopy.DraftClassificationId.HasValue)
            document.ClassificationId = workingCopy.DraftClassificationId;
        if (workingCopy.DraftImportanceId.HasValue)
            document.ImportanceId = workingCopy.DraftImportanceId;
        if (workingCopy.DraftDocumentTypeId.HasValue)
            document.DocumentTypeId = workingCopy.DraftDocumentTypeId;

        // Apply content type metadata changes from working copy
        if (!string.IsNullOrEmpty(workingCopy.DraftMetadataJson))
        {
            var draftMetadata = JsonSerializer.Deserialize<List<WorkingCopyMetadataItem>>(
                workingCopy.DraftMetadataJson, _jsonOptions);

            if (draftMetadata != null && draftMetadata.Count > 0)
            {
                // Get the content type ID from existing metadata
                var existingMetadata = await _contentTypeRepository.GetDocumentMetadataAsync(id);
                var contentTypeId = existingMetadata?.FirstOrDefault()?.ContentTypeId;

                if (contentTypeId.HasValue)
                {
                    // Convert working copy metadata to DocumentMetadata entities
                    var metadataEntities = draftMetadata.Select(m => new DocumentMetadata
                    {
                        DocumentId = id,
                        ContentTypeId = contentTypeId.Value,
                        FieldId = m.FieldId,
                        FieldName = m.FieldName,
                        Value = m.Value,
                        NumericValue = m.NumericValue,
                        DateValue = m.DateValue
                    }).ToList();

                    await _contentTypeRepository.SaveDocumentMetadataAsync(
                        id, contentTypeId.Value, metadataEntities, userId);
                }
            }
        }

        // Snapshot the updated metadata to the version (AFTER applying changes)
        await _versionMetadataRepository.SnapshotCurrentMetadataToVersionAsync(id, newVersionId);

        // Clear checkout flags (unless KeepCheckedOut)
        if (!dto.KeepCheckedOut)
        {
            document.IsCheckedOut = false;
            document.CheckedOutBy = null;
            document.CheckedOutAt = null;

            // Delete working copy
            await _workingCopyRepository.DeleteAsync(id);
        }
        else
        {
            // Reset working copy for next edit session
            workingCopy.DraftStoragePath = null;
            workingCopy.DraftSize = null;
            workingCopy.DraftContentType = null;
            workingCopy.DraftOriginalFileName = null;
            workingCopy.DraftIntegrityHash = null;
            workingCopy.DraftMetadataJson = null;
            workingCopy.LastModifiedAt = DateTime.UtcNow;
            await _workingCopyRepository.UpdateAsync(workingCopy);
        }

        await _documentRepository.UpdateAsync(document);

        await _activityLogService.LogActivityAsync(
            ActivityActions.CheckedIn, "Document", id, document.Name,
            $"Version {versionLabel} ({versionType})" + (dto.Comment != null ? $": {dto.Comment}" : ""),
            userId, null, null);

        return ServiceResult<DocumentVersionDto>.Ok(MapVersionToDto(newVersion),
            $"Document checked in successfully as version {versionLabel}");
    }

    public async Task<ServiceResult> DiscardCheckOutAsync(Guid id, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (!document.IsCheckedOut)
            return ServiceResult.Fail("Document is not checked out");

        if (document.CheckedOutBy != userId)
            return ServiceResult.Fail("You can only discard your own checkout");

        // Get and delete working copy
        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(id);
        if (workingCopy != null)
        {
            // Delete draft file if exists
            if (!string.IsNullOrEmpty(workingCopy.DraftStoragePath))
            {
                await _fileStorageService.DeleteFileAsync(workingCopy.DraftStoragePath);
            }
            await _workingCopyRepository.DeleteAsync(id);
        }

        // Clear checkout flags
        await _documentRepository.DiscardCheckOutAsync(id);

        await _activityLogService.LogActivityAsync(
            ActivityActions.DiscardedCheckout, "Document", id, document.Name,
            $"Checkout discarded. Originally checked out at {document.CheckedOutAt:yyyy-MM-dd HH:mm:ss} UTC",
            userId, null, null);

        return ServiceResult.Ok("Checkout discarded. All draft changes have been removed.");
    }

    public async Task<ServiceResult> ForceDiscardCheckOutAsync(Guid id, Guid adminUserId, string reason)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (!document.IsCheckedOut)
            return ServiceResult.Fail("Document is not checked out");

        var originalCheckoutUser = document.CheckedOutBy;

        // Get and delete working copy
        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(id);
        if (workingCopy != null)
        {
            if (!string.IsNullOrEmpty(workingCopy.DraftStoragePath))
            {
                await _fileStorageService.DeleteFileAsync(workingCopy.DraftStoragePath);
            }
            await _workingCopyRepository.DeleteAsync(id);
        }

        await _documentRepository.DiscardCheckOutAsync(id);

        await _activityLogService.LogActivityAsync(
            ActivityActions.DiscardedCheckout, "Document", id, document.Name,
            $"Force discarded by admin. Original user: {originalCheckoutUser}. Reason: {reason}",
            adminUserId, null, null);

        return ServiceResult.Ok("Checkout forcefully discarded by admin");
    }

    #endregion

    #region Working Copy Management

    public async Task<ServiceResult<WorkingCopyDto>> GetWorkingCopyAsync(Guid documentId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<WorkingCopyDto>.Fail("Document not found");

        if (!document.IsCheckedOut)
            return ServiceResult<WorkingCopyDto>.Fail("Document is not checked out");

        if (document.CheckedOutBy != userId)
            return ServiceResult<WorkingCopyDto>.Fail("Document is checked out by another user");

        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(documentId);

        // If no working copy exists (legacy checkout), create one now
        if (workingCopy == null)
        {
            workingCopy = new DocumentWorkingCopy
            {
                DocumentId = documentId,
                CheckedOutBy = userId,
                DraftName = document.Name,
                DraftDescription = document.Description,
                DraftClassificationId = document.ClassificationId,
                DraftImportanceId = document.ImportanceId,
                DraftDocumentTypeId = document.DocumentTypeId,
                AutoSaveEnabled = true
            };
            await _workingCopyRepository.CreateAsync(workingCopy);
        }

        var dto = new WorkingCopyDto
        {
            Id = workingCopy.Id,
            DocumentId = workingCopy.DocumentId,
            CheckedOutBy = workingCopy.CheckedOutBy,
            CheckedOutAt = workingCopy.CheckedOutAt,
            HasDraftFile = !string.IsNullOrEmpty(workingCopy.DraftStoragePath),
            DraftFileName = workingCopy.DraftOriginalFileName,
            DraftSize = workingCopy.DraftSize,
            DraftContentType = workingCopy.DraftContentType,
            DraftName = workingCopy.DraftName,
            DraftDescription = workingCopy.DraftDescription,
            DraftClassificationId = workingCopy.DraftClassificationId,
            DraftImportanceId = workingCopy.DraftImportanceId,
            DraftDocumentTypeId = workingCopy.DraftDocumentTypeId,
            LastModifiedAt = workingCopy.LastModifiedAt,
            AutoSaveEnabled = workingCopy.AutoSaveEnabled,
            HasUnsavedChanges = HasMetadataChanges(document, workingCopy) ||
                               !string.IsNullOrEmpty(workingCopy.DraftStoragePath)
        };

        // Parse metadata JSON if present
        if (!string.IsNullOrEmpty(workingCopy.DraftMetadataJson))
        {
            dto.DraftMetadata = JsonSerializer.Deserialize<List<WorkingCopyMetadataItem>>(workingCopy.DraftMetadataJson, _jsonOptions);
        }

        return ServiceResult<WorkingCopyDto>.Ok(dto);
    }

    public async Task<ServiceResult> SaveWorkingCopyAsync(Guid documentId, SaveWorkingCopyDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (!document.IsCheckedOut || document.CheckedOutBy != userId)
            return ServiceResult.Fail("Document is not checked out by you");

        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(documentId);

        // If no working copy exists (legacy checkout), create one now
        if (workingCopy == null)
        {
            workingCopy = new DocumentWorkingCopy
            {
                DocumentId = documentId,
                CheckedOutBy = userId,
                DraftName = document.Name,
                DraftDescription = document.Description,
                DraftClassificationId = document.ClassificationId,
                DraftImportanceId = document.ImportanceId,
                DraftDocumentTypeId = document.DocumentTypeId,
                AutoSaveEnabled = true
            };
            await _workingCopyRepository.CreateAsync(workingCopy);
        }

        if (dto.Name != null) workingCopy.DraftName = dto.Name;
        if (dto.Description != null) workingCopy.DraftDescription = dto.Description;
        if (dto.ClassificationId.HasValue) workingCopy.DraftClassificationId = dto.ClassificationId;
        if (dto.ImportanceId.HasValue) workingCopy.DraftImportanceId = dto.ImportanceId;
        if (dto.DocumentTypeId.HasValue) workingCopy.DraftDocumentTypeId = dto.DocumentTypeId;

        if (dto.Metadata != null)
        {
            workingCopy.DraftMetadataJson = JsonSerializer.Serialize(dto.Metadata, _jsonOptions);
        }

        workingCopy.LastModifiedAt = DateTime.UtcNow;
        await _workingCopyRepository.UpdateAsync(workingCopy);

        return ServiceResult.Ok("Draft saved successfully");
    }

    public async Task<ServiceResult> SaveWorkingCopyContentAsync(Guid documentId, Stream fileStream,
        string fileName, string contentType, Guid userId)
    {
        // Validate file before processing
        var validationResult = await _fileValidationService.ValidateFileAsync(fileStream, fileName, contentType);
        if (!validationResult.IsValid)
            return ServiceResult.Fail(validationResult.Error!);

        var validatedContentType = validationResult.ValidatedMimeType ?? contentType;

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        if (!document.IsCheckedOut || document.CheckedOutBy != userId)
            return ServiceResult.Fail("Document is not checked out by you");

        var workingCopy = await _workingCopyRepository.GetByDocumentIdAsync(documentId);
        if (workingCopy == null)
            return ServiceResult.Fail("Working copy not found");

        // Delete old draft file if exists
        if (!string.IsNullOrEmpty(workingCopy.DraftStoragePath))
        {
            await _fileStorageService.DeleteFileAsync(workingCopy.DraftStoragePath);
        }

        var storageResult = await _fileStorageService.SaveFileWithHashAsync(
            fileStream, fileName, documentId, document.CurrentVersion + 1);

        workingCopy.DraftStoragePath = storageResult.StoragePath;
        workingCopy.DraftSize = storageResult.Size;
        workingCopy.DraftContentType = validatedContentType;
        workingCopy.DraftOriginalFileName = fileName;
        workingCopy.DraftIntegrityHash = storageResult.IntegrityHash;
        workingCopy.LastModifiedAt = DateTime.UtcNow;

        await _workingCopyRepository.UpdateAsync(workingCopy);

        var message = "Draft content saved successfully";
        if (!string.IsNullOrEmpty(validationResult.Warning))
            message = $"{validationResult.Warning} {message}";

        return ServiceResult.Ok(message);
    }

    public async Task<ServiceResult<List<WorkingCopyDto>>> GetStaleCheckoutsAsync(int staleHours)
    {
        var staleCheckouts = await _workingCopyRepository.GetStaleCheckoutsAsync(staleHours);
        var dtos = staleCheckouts.Select(wc => new WorkingCopyDto
        {
            Id = wc.Id,
            DocumentId = wc.DocumentId,
            CheckedOutBy = wc.CheckedOutBy,
            CheckedOutAt = wc.CheckedOutAt,
            HasDraftFile = !string.IsNullOrEmpty(wc.DraftStoragePath),
            DraftFileName = wc.DraftOriginalFileName,
            DraftSize = wc.DraftSize,
            LastModifiedAt = wc.LastModifiedAt,
            AutoSaveEnabled = wc.AutoSaveEnabled
        }).ToList();

        return ServiceResult<List<WorkingCopyDto>>.Ok(dtos);
    }

    #endregion

    #region Version Comparison

    public async Task<ServiceResult<VersionComparisonDto>> CompareVersionsAsync(Guid documentId,
        Guid sourceVersionId, Guid targetVersionId)
    {
        var sourceVersion = await _versionRepository.GetByIdAsync(sourceVersionId);
        var targetVersion = await _versionRepository.GetByIdAsync(targetVersionId);

        if (sourceVersion == null || targetVersion == null)
            return ServiceResult<VersionComparisonDto>.Fail("One or both versions not found");

        if (sourceVersion.DocumentId != documentId || targetVersion.DocumentId != documentId)
            return ServiceResult<VersionComparisonDto>.Fail("Versions do not belong to this document");

        // Get metadata for both versions
        var metadataDict = await _versionMetadataRepository.GetMetadataForVersionsAsync(
            new[] { sourceVersionId, targetVersionId });

        var sourceMetadata = metadataDict.GetValueOrDefault(sourceVersionId, new List<DocumentVersionMetadata>());
        var targetMetadata = metadataDict.GetValueOrDefault(targetVersionId, new List<DocumentVersionMetadata>());

        // Build metadata diff
        var metadataDiffs = BuildMetadataDiff(sourceMetadata, targetMetadata);

        var comparison = new VersionComparisonDto
        {
            DocumentId = documentId,
            SourceVersion = new VersionSummaryDto
            {
                VersionId = sourceVersion.Id,
                VersionLabel = sourceVersion.VersionLabel ?? $"{sourceVersion.MajorVersion}.{sourceVersion.MinorVersion}",
                MajorVersion = sourceVersion.MajorVersion,
                MinorVersion = sourceVersion.MinorVersion,
                VersionType = sourceVersion.VersionType,
                Size = sourceVersion.Size,
                IntegrityHash = sourceVersion.IntegrityHash,
                ContentType = sourceVersion.ContentType,
                Comment = sourceVersion.Comment,
                CreatedBy = sourceVersion.CreatedBy,
                CreatedAt = sourceVersion.CreatedAt
            },
            TargetVersion = new VersionSummaryDto
            {
                VersionId = targetVersion.Id,
                VersionLabel = targetVersion.VersionLabel ?? $"{targetVersion.MajorVersion}.{targetVersion.MinorVersion}",
                MajorVersion = targetVersion.MajorVersion,
                MinorVersion = targetVersion.MinorVersion,
                VersionType = targetVersion.VersionType,
                Size = targetVersion.Size,
                IntegrityHash = targetVersion.IntegrityHash,
                ContentType = targetVersion.ContentType,
                Comment = targetVersion.Comment,
                CreatedBy = targetVersion.CreatedBy,
                CreatedAt = targetVersion.CreatedAt
            },
            ContentChanged = sourceVersion.IntegrityHash != targetVersion.IntegrityHash,
            MetadataChanged = metadataDiffs.Any(d => d.DiffType != DiffType.Unchanged),
            SizeDifference = targetVersion.Size - sourceVersion.Size,
            MetadataDifferences = metadataDiffs
        };

        return ServiceResult<VersionComparisonDto>.Ok(comparison);
    }

    public async Task<ServiceResult<DocumentVersionDto>> RestoreVersionAsync(Guid documentId,
        Guid versionId, RestoreVersionDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<DocumentVersionDto>.Fail("Document not found");

        if (document.IsCheckedOut)
            return ServiceResult<DocumentVersionDto>.Fail("Cannot restore version while document is checked out");

        var versionToRestore = await _versionRepository.GetByIdAsync(versionId);
        if (versionToRestore == null || versionToRestore.DocumentId != documentId)
            return ServiceResult<DocumentVersionDto>.Fail("Version not found");

        // Create new major version
        var newMajorVersion = document.CurrentMajorVersion + 1;
        var newVersionNumber = document.CurrentVersion + 1;
        var currentVersion = await _versionRepository.GetLatestVersionAsync(documentId);

        var newVersion = new DocumentVersion
        {
            DocumentId = documentId,
            VersionNumber = newVersionNumber,
            StoragePath = dto.RestoreContent ? versionToRestore.StoragePath : document.StoragePath,
            Size = dto.RestoreContent ? versionToRestore.Size : document.Size,
            Comment = dto.Comment ?? $"Restored from version {versionToRestore.VersionLabel}",
            CreatedBy = userId,
            IntegrityHash = dto.RestoreContent ? versionToRestore.IntegrityHash : document.IntegrityHash,
            HashAlgorithm = dto.RestoreContent ? versionToRestore.HashAlgorithm : document.HashAlgorithm,
            IntegrityVerifiedAt = DateTime.UtcNow,
            ContentType = dto.RestoreContent ? versionToRestore.ContentType : document.ContentType,
            OriginalFileName = versionToRestore.OriginalFileName,
            VersionType = "Major",
            MajorVersion = newMajorVersion,
            MinorVersion = 0,
            VersionLabel = $"{newMajorVersion}.0",
            IsContentChanged = dto.RestoreContent,
            IsMetadataChanged = dto.RestoreMetadata,
            PreviousVersionId = currentVersion?.Id,
            ChangeDescription = $"Restored from version {versionToRestore.VersionLabel}"
        };
        var newVersionId = await _versionRepository.CreateAsync(newVersion);

        // Restore metadata if requested
        if (dto.RestoreMetadata)
        {
            var oldMetadata = await _versionMetadataRepository.GetByVersionIdAsync(versionId);
            await _versionMetadataRepository.SaveVersionMetadataAsync(newVersionId, documentId, oldMetadata);
        }
        else
        {
            await _versionMetadataRepository.SnapshotCurrentMetadataToVersionAsync(documentId, newVersionId);
        }

        // Update document
        document.CurrentVersion = newVersionNumber;
        document.CurrentMajorVersion = newMajorVersion;
        document.CurrentMinorVersion = 0;
        document.CurrentVersionId = newVersionId;

        if (dto.RestoreContent)
        {
            document.StoragePath = versionToRestore.StoragePath;
            document.Size = versionToRestore.Size;
            document.IntegrityHash = versionToRestore.IntegrityHash;
            document.HashAlgorithm = versionToRestore.HashAlgorithm;
            document.ContentType = versionToRestore.ContentType;
        }

        document.ModifiedBy = userId;
        await _documentRepository.UpdateAsync(document);

        await _activityLogService.LogActivityAsync(
            ActivityActions.VersionRestored, "Document", documentId, document.Name,
            $"Restored to version {newMajorVersion}.0 from {versionToRestore.VersionLabel}",
            userId, null, null);

        return ServiceResult<DocumentVersionDto>.Ok(MapVersionToDto(newVersion),
            $"Version {versionToRestore.VersionLabel} restored as version {newMajorVersion}.0");
    }

    #endregion

    #region Private Helper Methods

    private static bool HasMetadataChanges(Document document, DocumentWorkingCopy workingCopy)
    {
        return workingCopy.DraftName != document.Name ||
               workingCopy.DraftDescription != document.Description ||
               workingCopy.DraftClassificationId != document.ClassificationId ||
               workingCopy.DraftImportanceId != document.ImportanceId ||
               workingCopy.DraftDocumentTypeId != document.DocumentTypeId ||
               !string.IsNullOrEmpty(workingCopy.DraftMetadataJson);
    }

    private static List<MetadataDiffItem> BuildMetadataDiff(
        List<DocumentVersionMetadata> sourceMetadata,
        List<DocumentVersionMetadata> targetMetadata)
    {
        var diffs = new List<MetadataDiffItem>();
        var sourceDict = sourceMetadata.ToDictionary(m => m.FieldId);
        var targetDict = targetMetadata.ToDictionary(m => m.FieldId);

        var allFieldIds = sourceDict.Keys.Union(targetDict.Keys);

        foreach (var fieldId in allFieldIds)
        {
            var hasSource = sourceDict.TryGetValue(fieldId, out var sourceItem);
            var hasTarget = targetDict.TryGetValue(fieldId, out var targetItem);

            DiffType diffType;
            string? oldValue = null;
            string? newValue = null;
            string fieldName = "";
            string displayName = "";

            if (hasSource && hasTarget)
            {
                fieldName = sourceItem!.FieldName;
                displayName = sourceItem.FieldName;
                oldValue = GetMetadataDisplayValue(sourceItem);
                newValue = GetMetadataDisplayValue(targetItem!);
                diffType = oldValue == newValue ? DiffType.Unchanged : DiffType.Modified;
            }
            else if (hasSource)
            {
                fieldName = sourceItem!.FieldName;
                displayName = sourceItem.FieldName;
                oldValue = GetMetadataDisplayValue(sourceItem);
                diffType = DiffType.Removed;
            }
            else
            {
                fieldName = targetItem!.FieldName;
                displayName = targetItem.FieldName;
                newValue = GetMetadataDisplayValue(targetItem);
                diffType = DiffType.Added;
            }

            diffs.Add(new MetadataDiffItem
            {
                FieldId = fieldId,
                FieldName = fieldName,
                DisplayName = displayName,
                OldValue = oldValue,
                NewValue = newValue,
                DiffType = diffType
            });
        }

        return diffs;
    }

    /// <summary>
    /// Gets the display value from a metadata item, checking Value, NumericValue, and DateValue.
    /// </summary>
    private static string? GetMetadataDisplayValue(DocumentVersionMetadata metadata)
    {
        // Check text value first
        if (!string.IsNullOrEmpty(metadata.Value))
            return metadata.Value;

        // Check numeric value
        if (metadata.NumericValue.HasValue)
            return metadata.NumericValue.Value.ToString();

        // Check date value
        if (metadata.DateValue.HasValue)
            return metadata.DateValue.Value.ToString("yyyy-MM-dd");

        return null;
    }

    private async Task<string> BuildDocumentPathAsync(Document document)
    {
        var pathParts = new List<string>();

        var folder = await _folderRepository.GetByIdAsync(document.FolderId);
        while (folder != null)
        {
            pathParts.Insert(0, folder.Name);
            if (folder.ParentFolderId.HasValue)
            {
                folder = await _folderRepository.GetByIdAsync(folder.ParentFolderId.Value);
            }
            else
            {
                break;
            }
        }

        pathParts.Add(document.Name + (document.Extension ?? ""));
        return "/" + string.Join("/", pathParts);
    }

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
            ModifiedAt = document.ModifiedAt
        };
    }

    private static DocumentDto MapToDtoWithNames(DocumentWithNames document)
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
            CheckedOutByName = document.CheckedOutByName,
            CheckedOutAt = document.CheckedOutAt,
            ClassificationId = document.ClassificationId,
            ImportanceId = document.ImportanceId,
            DocumentTypeId = document.DocumentTypeId,
            ContentTypeId = document.ContentTypeId,
            ContentTypeName = document.ContentTypeName,
            CreatedBy = document.CreatedBy,
            CreatedByName = document.CreatedByName,
            CreatedAt = document.CreatedAt,
            ModifiedAt = document.ModifiedAt,
            IsShortcut = document.IsShortcut,
            ShortcutId = document.ShortcutId,
            AttachmentCount = document.AttachmentCount
        };
    }

    private static DocumentVersionDto MapVersionToDto(DocumentVersion version)
    {
        return new DocumentVersionDto
        {
            Id = version.Id,
            DocumentId = version.DocumentId,
            VersionNumber = version.VersionNumber,
            Size = version.Size,
            Comment = version.Comment,
            CreatedBy = version.CreatedBy,
            CreatedAt = version.CreatedAt,
            VersionType = version.VersionType,
            VersionLabel = version.VersionLabel ?? $"{version.MajorVersion}.{version.MinorVersion}",
            MajorVersion = version.MajorVersion,
            MinorVersion = version.MinorVersion,
            IsContentChanged = version.IsContentChanged,
            IsMetadataChanged = version.IsMetadataChanged,
            ChangeDescription = version.ChangeDescription,
            ContentType = version.ContentType,
            OriginalFileName = version.OriginalFileName,
            IntegrityHash = version.IntegrityHash
        };
    }

    #endregion
}

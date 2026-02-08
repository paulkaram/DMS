using Dapper;
using DMS.DAL.Data;
using DMS.DAL.DTOs;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class ContentTypeDefinitionRepository : IContentTypeDefinitionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ContentTypeDefinitionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private string GetContentTypesTableName()
    {
        // Always use ContentTypeDefinitions - the table with Name, SortOrder, etc.
        // ContentTypes is the MIME types table (Extension, MimeType)
        return "ContentTypeDefinitions";
    }

    #region Content Type Definitions

    public async Task<IEnumerable<ContentTypeDefinition>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        var sql = $@"SELECT * FROM {tableName}
                    WHERE (@IncludeInactive = 1 OR IsActive = 1)
                    ORDER BY SortOrder, Name";
        return await connection.QueryAsync<ContentTypeDefinition>(sql, new { IncludeInactive = includeInactive });
    }

    public async Task<ContentTypeDefinition?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        return await connection.QueryFirstOrDefaultAsync<ContentTypeDefinition>(
            $"SELECT * FROM {tableName} WHERE Id = @Id", new { Id = id });
    }

    public async Task<ContentTypeDefinition?> GetByIdWithFieldsAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        var sql = $@"
            SELECT * FROM {tableName} WHERE Id = @Id;
            SELECT * FROM ContentTypeFields WHERE ContentTypeId = @Id AND IsActive = 1 ORDER BY SortOrder;";

        using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });
        var contentType = await multi.ReadFirstOrDefaultAsync<ContentTypeDefinition>();
        if (contentType != null)
        {
            contentType.Fields = (await multi.ReadAsync<ContentTypeField>()).ToList();
        }
        return contentType;
    }

    public async Task<ContentTypeDefinition?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        return await connection.QueryFirstOrDefaultAsync<ContentTypeDefinition>(
            $"SELECT * FROM {tableName} WHERE Name = @Name", new { Name = name });
    }

    public async Task<ContentTypeDefinition?> GetSystemDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        return await connection.QueryFirstOrDefaultAsync<ContentTypeDefinition>(
            $"SELECT * FROM {tableName} WHERE IsSystemDefault = 1 AND IsActive = 1");
    }

    public async Task<bool> SetSystemDefaultAsync(Guid contentTypeId, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Clear existing system default
            await connection.ExecuteAsync(
                $"UPDATE {tableName} SET IsSystemDefault = 0, ModifiedBy = @UserId, ModifiedAt = @Now WHERE IsSystemDefault = 1",
                new { UserId = userId, Now = DateTime.UtcNow },
                transaction);

            // Set new system default
            var result = await connection.ExecuteAsync(
                $"UPDATE {tableName} SET IsSystemDefault = 1, ModifiedBy = @UserId, ModifiedAt = @Now WHERE Id = @Id",
                new { Id = contentTypeId, UserId = userId, Now = DateTime.UtcNow },
                transaction);

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<bool> ClearSystemDefaultAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        return await connection.ExecuteAsync(
            $"UPDATE {tableName} SET IsSystemDefault = 0, ModifiedBy = @UserId, ModifiedAt = @Now WHERE IsSystemDefault = 1",
            new { UserId = userId, Now = DateTime.UtcNow }) > 0;
    }

    public async Task<Guid> CreateAsync(ContentTypeDefinition contentType)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        contentType.Id = Guid.NewGuid();
        contentType.CreatedAt = DateTime.UtcNow;

        var sql = $@"INSERT INTO {tableName}
                    (Id, Name, Description, Icon, Color, Category, AllowOnFolders, AllowOnDocuments, IsRequired, IsSystemDefault, IsActive, SortOrder, CreatedBy, CreatedAt)
                    VALUES (@Id, @Name, @Description, @Icon, @Color, @Category, @AllowOnFolders, @AllowOnDocuments, @IsRequired, @IsSystemDefault, @IsActive, @SortOrder, @CreatedBy, @CreatedAt)";

        await connection.ExecuteAsync(sql, contentType);
        return contentType.Id;
    }

    public async Task<bool> UpdateAsync(ContentTypeDefinition contentType)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        contentType.ModifiedAt = DateTime.UtcNow;

        var sql = $@"UPDATE {tableName} SET
                    Name = @Name, Description = @Description, Icon = @Icon, Color = @Color, Category = @Category,
                    AllowOnFolders = @AllowOnFolders, AllowOnDocuments = @AllowOnDocuments,
                    IsRequired = @IsRequired, IsSystemDefault = @IsSystemDefault, IsActive = @IsActive, SortOrder = @SortOrder,
                    ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
                    WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, contentType) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        // Soft delete - just mark as inactive
        return await connection.ExecuteAsync(
            $"UPDATE {tableName} SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow }) > 0;
    }

    #endregion

    #region Content Type Fields

    public async Task<IEnumerable<ContentTypeField>> GetFieldsAsync(Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ContentTypeField>(
            "SELECT * FROM ContentTypeFields WHERE ContentTypeId = @ContentTypeId AND IsActive = 1 ORDER BY SortOrder",
            new { ContentTypeId = contentTypeId });
    }

    public async Task<ContentTypeField?> GetFieldByIdAsync(Guid fieldId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ContentTypeField>(
            "SELECT * FROM ContentTypeFields WHERE Id = @Id", new { Id = fieldId });
    }

    public async Task<Guid> CreateFieldAsync(ContentTypeField field)
    {
        using var connection = _connectionFactory.CreateConnection();
        field.Id = Guid.NewGuid();
        field.CreatedAt = DateTime.UtcNow;

        // Get max sort order
        var maxOrder = await connection.ExecuteScalarAsync<int?>(
            "SELECT MAX(SortOrder) FROM ContentTypeFields WHERE ContentTypeId = @ContentTypeId",
            new { field.ContentTypeId });
        field.SortOrder = (maxOrder ?? 0) + 1;

        var sql = @"INSERT INTO ContentTypeFields
                    (Id, ContentTypeId, FieldName, DisplayName, Description, FieldType, IsRequired, IsReadOnly,
                     ShowInList, IsSearchable, DefaultValue, ValidationRules, LookupName, Options, SortOrder,
                     GroupName, ColumnSpan, IsActive, CreatedAt)
                    VALUES (@Id, @ContentTypeId, @FieldName, @DisplayName, @Description, @FieldType, @IsRequired, @IsReadOnly,
                            @ShowInList, @IsSearchable, @DefaultValue, @ValidationRules, @LookupName, @Options, @SortOrder,
                            @GroupName, @ColumnSpan, @IsActive, @CreatedAt)";

        await connection.ExecuteAsync(sql, field);
        return field.Id;
    }

    public async Task<bool> UpdateFieldAsync(ContentTypeField field)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"UPDATE ContentTypeFields SET
                    FieldName = @FieldName, DisplayName = @DisplayName, Description = @Description,
                    FieldType = @FieldType, IsRequired = @IsRequired, IsReadOnly = @IsReadOnly,
                    ShowInList = @ShowInList, IsSearchable = @IsSearchable, DefaultValue = @DefaultValue,
                    ValidationRules = @ValidationRules, LookupName = @LookupName, Options = @Options,
                    SortOrder = @SortOrder, GroupName = @GroupName, ColumnSpan = @ColumnSpan, IsActive = @IsActive
                    WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, field) > 0;
    }

    public async Task<bool> DeleteFieldAsync(Guid fieldId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "UPDATE ContentTypeFields SET IsActive = 0 WHERE Id = @Id",
            new { Id = fieldId }) > 0;
    }

    public async Task<bool> ReorderFieldsAsync(Guid contentTypeId, List<Guid> fieldIds)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            for (int i = 0; i < fieldIds.Count; i++)
            {
                await connection.ExecuteAsync(
                    "UPDATE ContentTypeFields SET SortOrder = @Order WHERE Id = @Id AND ContentTypeId = @ContentTypeId",
                    new { Id = fieldIds[i], ContentTypeId = contentTypeId, Order = i + 1 },
                    transaction);
            }
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    #endregion

    #region Document Metadata

    public async Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentMetadata>(
            "SELECT * FROM DocumentMetadata WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentMetadata>(
            "SELECT * FROM DocumentMetadata WHERE DocumentId = @DocumentId AND ContentTypeId = @ContentTypeId",
            new { DocumentId = documentId, ContentTypeId = contentTypeId });
    }

    public async Task<bool> SaveDocumentMetadataAsync(Guid documentId, Guid contentTypeId, List<DocumentMetadata> metadata, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete existing metadata for this document/content type
            await connection.ExecuteAsync(
                "DELETE FROM DocumentMetadata WHERE DocumentId = @DocumentId AND ContentTypeId = @ContentTypeId",
                new { DocumentId = documentId, ContentTypeId = contentTypeId },
                transaction);

            // Insert new metadata
            foreach (var item in metadata)
            {
                item.Id = Guid.NewGuid();
                item.DocumentId = documentId;
                item.ContentTypeId = contentTypeId;
                item.CreatedBy = userId;
                item.CreatedAt = DateTime.UtcNow;

                var sql = @"INSERT INTO DocumentMetadata
                            (Id, DocumentId, ContentTypeId, FieldId, FieldName, Value, NumericValue, DateValue, CreatedBy, CreatedAt)
                            VALUES (@Id, @DocumentId, @ContentTypeId, @FieldId, @FieldName, @Value, @NumericValue, @DateValue, @CreatedBy, @CreatedAt)";

                await connection.ExecuteAsync(sql, item, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<bool> DeleteDocumentMetadataAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM DocumentMetadata WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId }) > 0;
    }

    public async Task<bool> DeleteDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM DocumentMetadata WHERE DocumentId = @DocumentId AND ContentTypeId = @ContentTypeId",
            new { DocumentId = documentId, ContentTypeId = contentTypeId }) > 0;
    }

    #endregion

    #region Folder Content Type Assignments

    public async Task<IEnumerable<FolderContentTypeAssignment>> GetFolderContentTypesAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        var sql = $@"SELECT a.*, ct.Name as ContentTypeName, ct.Description as ContentTypeDescription,
                           ct.Icon as ContentTypeIcon, ct.Color as ContentTypeColor
                    FROM FolderContentTypeAssignments a
                    INNER JOIN {tableName} ct ON a.ContentTypeId = ct.Id
                    WHERE a.FolderId = @FolderId AND ct.IsActive = 1
                    ORDER BY a.DisplayOrder, ct.Name";

        return await connection.QueryAsync<FolderContentTypeAssignment>(sql, new { FolderId = folderId });
    }

    public async Task<IEnumerable<ContentTypeDefinition>> GetAvailableContentTypesForFolderAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        // Get content types that allow folders and aren't already assigned
        var sql = $@"SELECT * FROM {tableName}
                    WHERE IsActive = 1 AND AllowOnFolders = 1
                    AND Id NOT IN (SELECT ContentTypeId FROM FolderContentTypeAssignments WHERE FolderId = @FolderId)
                    ORDER BY SortOrder, Name";

        return await connection.QueryAsync<ContentTypeDefinition>(sql, new { FolderId = folderId });
    }

    public async Task<Guid> AssignContentTypeToFolderAsync(FolderContentTypeAssignment assignment)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            assignment.Id = Guid.NewGuid();
            assignment.CreatedAt = DateTime.UtcNow;

            // If setting as default, clear existing default first
            if (assignment.IsDefault)
            {
                await connection.ExecuteAsync(
                    "UPDATE FolderContentTypeAssignments SET IsDefault = 0 WHERE FolderId = @FolderId AND IsDefault = 1",
                    new { assignment.FolderId },
                    transaction);
            }

            var sql = @"INSERT INTO FolderContentTypeAssignments
                        (Id, FolderId, ContentTypeId, IsRequired, IsDefault, InheritToChildren, DisplayOrder, CreatedBy, CreatedAt)
                        VALUES (@Id, @FolderId, @ContentTypeId, @IsRequired, @IsDefault, @InheritToChildren, @DisplayOrder, @CreatedBy, @CreatedAt)";

            await connection.ExecuteAsync(sql, assignment, transaction);
            transaction.Commit();
            return assignment.Id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RemoveContentTypeFromFolderAsync(Guid assignmentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM FolderContentTypeAssignments WHERE Id = @Id",
            new { Id = assignmentId }) > 0;
    }

    public async Task<bool> RemoveContentTypeFromFolderAsync(Guid folderId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM FolderContentTypeAssignments WHERE FolderId = @FolderId AND ContentTypeId = @ContentTypeId",
            new { FolderId = folderId, ContentTypeId = contentTypeId }) > 0;
    }

    public async Task<bool> UpdateFolderAssignmentAsync(Guid folderId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // If setting as default, clear existing default first
            if (isDefault)
            {
                await connection.ExecuteAsync(
                    "UPDATE FolderContentTypeAssignments SET IsDefault = 0 WHERE FolderId = @FolderId AND ContentTypeId != @ContentTypeId",
                    new { FolderId = folderId, ContentTypeId = contentTypeId },
                    transaction);
            }

            var result = await connection.ExecuteAsync(
                @"UPDATE FolderContentTypeAssignments SET
                    IsRequired = @IsRequired, IsDefault = @IsDefault,
                    InheritToChildren = @InheritToChildren, DisplayOrder = @DisplayOrder
                  WHERE FolderId = @FolderId AND ContentTypeId = @ContentTypeId",
                new { FolderId = folderId, ContentTypeId = contentTypeId, IsRequired = isRequired, IsDefault = isDefault, InheritToChildren = inheritToChildren, DisplayOrder = displayOrder },
                transaction);

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<bool> SetFolderDefaultContentTypeAsync(Guid folderId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Clear existing default
            await connection.ExecuteAsync(
                "UPDATE FolderContentTypeAssignments SET IsDefault = 0 WHERE FolderId = @FolderId",
                new { FolderId = folderId },
                transaction);

            // Set new default
            var result = await connection.ExecuteAsync(
                "UPDATE FolderContentTypeAssignments SET IsDefault = 1 WHERE FolderId = @FolderId AND ContentTypeId = @ContentTypeId",
                new { FolderId = folderId, ContentTypeId = contentTypeId },
                transaction);

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    #endregion

    #region Cabinet Content Type Assignments

    public async Task<IEnumerable<CabinetContentTypeAssignment>> GetCabinetContentTypesAsync(Guid cabinetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();
        var sql = $@"SELECT a.*, ct.Name as ContentTypeName, ct.Description as ContentTypeDescription,
                           ct.Icon as ContentTypeIcon, ct.Color as ContentTypeColor
                    FROM CabinetContentTypeAssignments a
                    INNER JOIN {tableName} ct ON a.ContentTypeId = ct.Id
                    WHERE a.CabinetId = @CabinetId AND ct.IsActive = 1
                    ORDER BY a.DisplayOrder, ct.Name";

        return await connection.QueryAsync<CabinetContentTypeAssignment>(sql, new { CabinetId = cabinetId });
    }

    public async Task<Guid> AssignContentTypeToCabinetAsync(CabinetContentTypeAssignment assignment)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            assignment.Id = Guid.NewGuid();
            assignment.CreatedAt = DateTime.UtcNow;

            // If setting as default, clear existing default first
            if (assignment.IsDefault)
            {
                await connection.ExecuteAsync(
                    "UPDATE CabinetContentTypeAssignments SET IsDefault = 0 WHERE CabinetId = @CabinetId AND IsDefault = 1",
                    new { assignment.CabinetId },
                    transaction);
            }

            var sql = @"INSERT INTO CabinetContentTypeAssignments
                        (Id, CabinetId, ContentTypeId, IsRequired, IsDefault, InheritToChildren, DisplayOrder, CreatedBy, CreatedAt)
                        VALUES (@Id, @CabinetId, @ContentTypeId, @IsRequired, @IsDefault, @InheritToChildren, @DisplayOrder, @CreatedBy, @CreatedAt)";

            await connection.ExecuteAsync(sql, assignment, transaction);
            transaction.Commit();
            return assignment.Id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RemoveContentTypeFromCabinetAsync(Guid cabinetId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM CabinetContentTypeAssignments WHERE CabinetId = @CabinetId AND ContentTypeId = @ContentTypeId",
            new { CabinetId = cabinetId, ContentTypeId = contentTypeId }) > 0;
    }

    public async Task<bool> UpdateCabinetAssignmentAsync(Guid cabinetId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // If setting as default, clear existing default first
            if (isDefault)
            {
                await connection.ExecuteAsync(
                    "UPDATE CabinetContentTypeAssignments SET IsDefault = 0 WHERE CabinetId = @CabinetId AND ContentTypeId != @ContentTypeId",
                    new { CabinetId = cabinetId, ContentTypeId = contentTypeId },
                    transaction);
            }

            var result = await connection.ExecuteAsync(
                @"UPDATE CabinetContentTypeAssignments SET
                    IsRequired = @IsRequired, IsDefault = @IsDefault,
                    InheritToChildren = @InheritToChildren, DisplayOrder = @DisplayOrder
                  WHERE CabinetId = @CabinetId AND ContentTypeId = @ContentTypeId",
                new { CabinetId = cabinetId, ContentTypeId = contentTypeId, IsRequired = isRequired, IsDefault = isDefault, InheritToChildren = inheritToChildren, DisplayOrder = displayOrder },
                transaction);

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<bool> SetCabinetDefaultContentTypeAsync(Guid cabinetId, Guid contentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Clear existing default
            await connection.ExecuteAsync(
                "UPDATE CabinetContentTypeAssignments SET IsDefault = 0 WHERE CabinetId = @CabinetId",
                new { CabinetId = cabinetId },
                transaction);

            // Set new default
            var result = await connection.ExecuteAsync(
                "UPDATE CabinetContentTypeAssignments SET IsDefault = 1 WHERE CabinetId = @CabinetId AND ContentTypeId = @ContentTypeId",
                new { CabinetId = cabinetId, ContentTypeId = contentTypeId },
                transaction);

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    #endregion

    #region Effective Content Types

    public async Task<IEnumerable<EffectiveContentTypeDto>> GetEffectiveContentTypesAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tableName = GetContentTypesTableName();

        // Get folder info including cabinet and parent chain
        var folder = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, CabinetId, ParentFolderId, Name, BreakContentTypeInheritance FROM Folders WHERE Id = @FolderId",
            new { FolderId = folderId });

        if (folder == null)
            return Enumerable.Empty<EffectiveContentTypeDto>();

        var results = new List<EffectiveContentTypeDto>();
        var addedContentTypeIds = new HashSet<Guid>();

        // 1. Get direct folder assignments (highest priority)
        var directAssignments = await connection.QueryAsync<EffectiveContentTypeDto>($@"
            SELECT a.ContentTypeId, ct.Name, ct.Description, ct.Icon, ct.Color, ct.Category,
                   a.IsRequired, a.IsDefault, a.DisplayOrder,
                   'Direct' as Source, NULL as SourceName, NULL as SourceId
            FROM FolderContentTypeAssignments a
            INNER JOIN {tableName} ct ON a.ContentTypeId = ct.Id
            WHERE a.FolderId = @FolderId AND ct.IsActive = 1
            ORDER BY a.DisplayOrder, ct.Name",
            new { FolderId = folderId });

        foreach (var assignment in directAssignments)
        {
            results.Add(assignment);
            addedContentTypeIds.Add(assignment.ContentTypeId);
        }

        // Check if folder breaks inheritance
        bool breakInheritance = folder.BreakContentTypeInheritance == true;

        if (!breakInheritance)
        {
            // 2. Get inherited from parent folders
            var currentParentId = (Guid?)folder.ParentFolderId;
            while (currentParentId.HasValue)
            {
                var parentFolder = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, Name, ParentFolderId, BreakContentTypeInheritance FROM Folders WHERE Id = @Id",
                    new { Id = currentParentId.Value });

                if (parentFolder == null || parentFolder.BreakContentTypeInheritance == true)
                    break;

                var inheritedAssignments = await connection.QueryAsync<EffectiveContentTypeDto>($@"
                    SELECT a.ContentTypeId, ct.Name, ct.Description, ct.Icon, ct.Color, ct.Category,
                           a.IsRequired, a.IsDefault, a.DisplayOrder,
                           'Inherited' as Source, @ParentName as SourceName, @ParentId as SourceId
                    FROM FolderContentTypeAssignments a
                    INNER JOIN {tableName} ct ON a.ContentTypeId = ct.Id
                    WHERE a.FolderId = @ParentFolderId AND a.InheritToChildren = 1 AND ct.IsActive = 1
                    ORDER BY a.DisplayOrder, ct.Name",
                    new { ParentFolderId = currentParentId.Value, ParentName = (string)parentFolder.Name, ParentId = currentParentId.Value });

                foreach (var assignment in inheritedAssignments)
                {
                    if (!addedContentTypeIds.Contains(assignment.ContentTypeId))
                    {
                        results.Add(assignment);
                        addedContentTypeIds.Add(assignment.ContentTypeId);
                    }
                }

                currentParentId = (Guid?)parentFolder.ParentFolderId;
            }

            // 3. Get cabinet assignments (lowest priority)
            var cabinetId = (Guid)folder.CabinetId;
            var cabinet = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id, Name FROM Cabinets WHERE Id = @Id",
                new { Id = cabinetId });

            if (cabinet != null)
            {
                var cabinetAssignments = await connection.QueryAsync<EffectiveContentTypeDto>($@"
                    SELECT a.ContentTypeId, ct.Name, ct.Description, ct.Icon, ct.Color, ct.Category,
                           a.IsRequired, a.IsDefault, a.DisplayOrder,
                           'Cabinet' as Source, @CabinetName as SourceName, @CabinetId as SourceId
                    FROM CabinetContentTypeAssignments a
                    INNER JOIN {tableName} ct ON a.ContentTypeId = ct.Id
                    WHERE a.CabinetId = @CabinetId AND a.InheritToChildren = 1 AND ct.IsActive = 1
                    ORDER BY a.DisplayOrder, ct.Name",
                    new { CabinetId = cabinetId, CabinetName = (string)cabinet.Name });

                foreach (var assignment in cabinetAssignments)
                {
                    if (!addedContentTypeIds.Contains(assignment.ContentTypeId))
                    {
                        results.Add(assignment);
                        addedContentTypeIds.Add(assignment.ContentTypeId);
                    }
                }
            }
        }

        // 4. If no content types found, add system default
        if (results.Count == 0)
        {
            var systemDefault = await connection.QueryFirstOrDefaultAsync<EffectiveContentTypeDto>($@"
                SELECT Id as ContentTypeId, Name, Description, Icon, Color, Category,
                       0 as IsRequired, 1 as IsDefault, 0 as DisplayOrder,
                       'SystemDefault' as Source, 'System' as SourceName, NULL as SourceId
                FROM {tableName}
                WHERE IsSystemDefault = 1 AND IsActive = 1");

            if (systemDefault != null)
            {
                results.Add(systemDefault);
                addedContentTypeIds.Add(systemDefault.ContentTypeId);
            }
        }

        // Load fields for each content type
        foreach (var ct in results)
        {
            var fields = await connection.QueryAsync<ContentTypeFieldDto>(
                @"SELECT Id, ContentTypeId, FieldName, DisplayName, Description, FieldType,
                         IsRequired, IsReadOnly, ShowInList, IsSearchable, DefaultValue,
                         ValidationRules, LookupName, Options, SortOrder, GroupName, ColumnSpan
                  FROM ContentTypeFields
                  WHERE ContentTypeId = @Id AND IsActive = 1
                  ORDER BY SortOrder",
                new { Id = ct.ContentTypeId });
            ct.Fields = fields.ToList();
        }

        return results.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name);
    }

    public async Task<FolderContentTypeInfoDto?> GetFolderContentTypeInfoAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Get folder info
        var folder = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, Name, BreakContentTypeInheritance FROM Folders WHERE Id = @FolderId",
            new { FolderId = folderId });

        if (folder == null)
            return null;

        // Get effective content types
        var contentTypes = (await GetEffectiveContentTypesAsync(folderId)).ToList();

        // Find default
        var defaultCt = contentTypes.FirstOrDefault(x => x.IsDefault);

        return new FolderContentTypeInfoDto
        {
            FolderId = folderId,
            FolderName = folder.Name,
            HasRequiredContentTypes = contentTypes.Any(x => x.IsRequired),
            HasContentTypes = contentTypes.Count > 0,
            DefaultContentTypeId = defaultCt?.ContentTypeId,
            DefaultContentTypeName = defaultCt?.Name,
            TotalContentTypes = contentTypes.Count,
            BreaksInheritance = folder.BreakContentTypeInheritance == true,
            ContentTypes = contentTypes
        };
    }

    #endregion
}

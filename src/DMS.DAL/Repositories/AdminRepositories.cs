using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class BookmarkRepository : IBookmarkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BookmarkRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Bookmark>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Bookmark>(
            "SELECT * FROM Bookmarks WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY SortOrder, Name",
            new { IncludeInactive = includeInactive });
    }

    public async Task<Bookmark?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Bookmark>("SELECT * FROM Bookmarks WHERE Id = @Id", new { Id = id });
    }

    public async Task<Bookmark?> GetByPlaceholderAsync(string placeholder)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Bookmark>(
            "SELECT * FROM Bookmarks WHERE Placeholder = @Placeholder AND IsActive = 1", new { Placeholder = placeholder });
    }

    public async Task<Guid> CreateAsync(Bookmark bookmark)
    {
        using var connection = _connectionFactory.CreateConnection();
        bookmark.Id = Guid.NewGuid();
        bookmark.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO Bookmarks (Id, Name, Placeholder, Description, DefaultValue, DataType, LookupName, IsSystem, IsActive, SortOrder, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Placeholder, @Description, @DefaultValue, @DataType, @LookupName, @IsSystem, @IsActive, @SortOrder, @CreatedBy, @CreatedAt)", bookmark);
        return bookmark.Id;
    }

    public async Task<bool> UpdateAsync(Bookmark bookmark)
    {
        using var connection = _connectionFactory.CreateConnection();
        bookmark.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE Bookmarks SET Name=@Name, Placeholder=@Placeholder, Description=@Description, DefaultValue=@DefaultValue,
            DataType=@DataType, LookupName=@LookupName, IsSystem=@IsSystem, IsActive=@IsActive, SortOrder=@SortOrder, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", bookmark) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE Bookmarks SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class CaseRepository : ICaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CaseRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Case>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Case>(@"SELECT c.*, u.DisplayName as AssignedToUserName, f.Name as FolderName
            FROM Cases c LEFT JOIN Users u ON c.AssignedToUserId = u.Id LEFT JOIN Folders f ON c.FolderId = f.Id
            WHERE (@IncludeInactive = 1 OR c.IsActive = 1) ORDER BY c.CreatedAt DESC", new { IncludeInactive = includeInactive });
    }

    public async Task<Case?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Case>(@"SELECT c.*, u.DisplayName as AssignedToUserName, f.Name as FolderName
            FROM Cases c LEFT JOIN Users u ON c.AssignedToUserId = u.Id LEFT JOIN Folders f ON c.FolderId = f.Id WHERE c.Id = @Id", new { Id = id });
    }

    public async Task<Case?> GetByCaseNumberAsync(string caseNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Case>("SELECT * FROM Cases WHERE CaseNumber = @CaseNumber", new { CaseNumber = caseNumber });
    }

    public async Task<IEnumerable<Case>> GetByStatusAsync(string status)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Case>("SELECT * FROM Cases WHERE Status = @Status AND IsActive = 1 ORDER BY CreatedAt DESC", new { Status = status });
    }

    public async Task<IEnumerable<Case>> GetByAssigneeAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Case>("SELECT * FROM Cases WHERE AssignedToUserId = @UserId AND IsActive = 1 ORDER BY CreatedAt DESC", new { UserId = userId });
    }

    public async Task<Guid> CreateAsync(Case caseEntity)
    {
        using var connection = _connectionFactory.CreateConnection();
        caseEntity.Id = Guid.NewGuid();
        caseEntity.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO Cases (Id, CaseNumber, Title, Description, Status, Priority, AssignedToUserId, FolderId, DueDate, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @CaseNumber, @Title, @Description, @Status, @Priority, @AssignedToUserId, @FolderId, @DueDate, @IsActive, @CreatedBy, @CreatedAt)", caseEntity);
        return caseEntity.Id;
    }

    public async Task<bool> UpdateAsync(Case caseEntity)
    {
        using var connection = _connectionFactory.CreateConnection();
        caseEntity.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE Cases SET CaseNumber=@CaseNumber, Title=@Title, Description=@Description, Status=@Status, Priority=@Priority,
            AssignedToUserId=@AssignedToUserId, FolderId=@FolderId, DueDate=@DueDate, ClosedDate=@ClosedDate, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", caseEntity) > 0;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var closedDate = status == "Closed" || status == "Archived" ? DateTime.UtcNow : (DateTime?)null;
        return await connection.ExecuteAsync("UPDATE Cases SET Status=@Status, ClosedDate=@ClosedDate, ModifiedBy=@UserId, ModifiedAt=@Now WHERE Id=@Id",
            new { Id = id, Status = status, ClosedDate = closedDate, UserId = userId, Now = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE Cases SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class EndpointRepository : IEndpointRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EndpointRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<ServiceEndpoint>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ServiceEndpoint>("SELECT * FROM Endpoints WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY Name", new { IncludeInactive = includeInactive });
    }

    public async Task<ServiceEndpoint?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ServiceEndpoint>("SELECT * FROM Endpoints WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<ServiceEndpoint>> GetByTypeAsync(string endpointType)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ServiceEndpoint>("SELECT * FROM Endpoints WHERE EndpointType = @EndpointType AND IsActive = 1", new { EndpointType = endpointType });
    }

    public async Task<Guid> CreateAsync(ServiceEndpoint endpoint)
    {
        using var connection = _connectionFactory.CreateConnection();
        endpoint.Id = Guid.NewGuid();
        endpoint.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO Endpoints (Id, Name, Url, Description, EndpointType, AuthType, AuthConfig, TimeoutSeconds, RetryCount, Headers, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Url, @Description, @EndpointType, @AuthType, @AuthConfig, @TimeoutSeconds, @RetryCount, @Headers, @IsActive, @CreatedBy, @CreatedAt)", endpoint);
        return endpoint.Id;
    }

    public async Task<bool> UpdateAsync(ServiceEndpoint endpoint)
    {
        using var connection = _connectionFactory.CreateConnection();
        endpoint.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE Endpoints SET Name=@Name, Url=@Url, Description=@Description, EndpointType=@EndpointType, AuthType=@AuthType,
            AuthConfig=@AuthConfig, TimeoutSeconds=@TimeoutSeconds, RetryCount=@RetryCount, Headers=@Headers, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", endpoint) > 0;
    }

    public async Task<bool> UpdateHealthStatusAsync(Guid id, string status)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE Endpoints SET LastHealthCheck=@Now, LastHealthStatus=@Status WHERE Id=@Id",
            new { Id = id, Status = status, Now = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE Endpoints SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class ExportConfigRepository : IExportConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExportConfigRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<ExportConfig>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ExportConfig>("SELECT * FROM ExportConfigs WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY Name", new { IncludeInactive = includeInactive });
    }

    public async Task<ExportConfig?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ExportConfig>("SELECT * FROM ExportConfigs WHERE Id = @Id", new { Id = id });
    }

    public async Task<ExportConfig?> GetDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ExportConfig>("SELECT * FROM ExportConfigs WHERE IsDefault = 1 AND IsActive = 1");
    }

    public async Task<Guid> CreateAsync(ExportConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO ExportConfigs (Id, Name, Description, ExportFormat, IncludeMetadata, IncludeVersions, IncludeAuditTrail, FlattenFolders, NamingPattern, WatermarkText, MaxFileSizeMB, IsDefault, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @ExportFormat, @IncludeMetadata, @IncludeVersions, @IncludeAuditTrail, @FlattenFolders, @NamingPattern, @WatermarkText, @MaxFileSizeMB, @IsDefault, @IsActive, @CreatedBy, @CreatedAt)", config);
        return config.Id;
    }

    public async Task<bool> UpdateAsync(ExportConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE ExportConfigs SET Name=@Name, Description=@Description, ExportFormat=@ExportFormat, IncludeMetadata=@IncludeMetadata, IncludeVersions=@IncludeVersions,
            IncludeAuditTrail=@IncludeAuditTrail, FlattenFolders=@FlattenFolders, NamingPattern=@NamingPattern, WatermarkText=@WatermarkText, MaxFileSizeMB=@MaxFileSizeMB, IsDefault=@IsDefault, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", config) > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("UPDATE ExportConfigs SET IsDefault=0", transaction: transaction);
            await connection.ExecuteAsync("UPDATE ExportConfigs SET IsDefault=1 WHERE Id=@Id", new { Id = id }, transaction);
            transaction.Commit();
            return true;
        }
        catch { transaction.Rollback(); return false; }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE ExportConfigs SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class NamingConventionRepository : INamingConventionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NamingConventionRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<NamingConvention>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<NamingConvention>(@"SELECT nc.*, f.Name as FolderName, dt.Name as DocumentTypeName
            FROM NamingConventions nc LEFT JOIN Folders f ON nc.FolderId = f.Id LEFT JOIN DocumentTypes dt ON nc.DocumentTypeId = dt.Id
            WHERE (@IncludeInactive = 1 OR nc.IsActive = 1) ORDER BY nc.SortOrder, nc.Name", new { IncludeInactive = includeInactive });
    }

    public async Task<NamingConvention?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NamingConvention>(@"SELECT nc.*, f.Name as FolderName, dt.Name as DocumentTypeName
            FROM NamingConventions nc LEFT JOIN Folders f ON nc.FolderId = f.Id LEFT JOIN DocumentTypes dt ON nc.DocumentTypeId = dt.Id WHERE nc.Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<NamingConvention>> GetByFolderAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<NamingConvention>("SELECT * FROM NamingConventions WHERE FolderId = @FolderId AND IsActive = 1 ORDER BY SortOrder", new { FolderId = folderId });
    }

    public async Task<NamingConvention?> GetApplicableAsync(Guid? folderId, Guid? documentTypeId, string appliesTo)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NamingConvention>(@"SELECT TOP 1 * FROM NamingConventions
            WHERE IsActive = 1 AND (AppliesTo = @AppliesTo OR AppliesTo = 'Both')
            AND (FolderId = @FolderId OR FolderId IS NULL) AND (DocumentTypeId = @DocumentTypeId OR DocumentTypeId IS NULL)
            ORDER BY CASE WHEN FolderId = @FolderId THEN 0 ELSE 1 END, CASE WHEN DocumentTypeId = @DocumentTypeId THEN 0 ELSE 1 END, SortOrder",
            new { FolderId = folderId, DocumentTypeId = documentTypeId, AppliesTo = appliesTo });
    }

    public async Task<Guid> CreateAsync(NamingConvention convention)
    {
        using var connection = _connectionFactory.CreateConnection();
        convention.Id = Guid.NewGuid();
        convention.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO NamingConventions (Id, Name, Description, Pattern, AppliesTo, FolderId, DocumentTypeId, IsRequired, AutoGenerate, Separator, IsActive, SortOrder, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @Pattern, @AppliesTo, @FolderId, @DocumentTypeId, @IsRequired, @AutoGenerate, @Separator, @IsActive, @SortOrder, @CreatedBy, @CreatedAt)", convention);
        return convention.Id;
    }

    public async Task<bool> UpdateAsync(NamingConvention convention)
    {
        using var connection = _connectionFactory.CreateConnection();
        convention.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE NamingConventions SET Name=@Name, Description=@Description, Pattern=@Pattern, AppliesTo=@AppliesTo, FolderId=@FolderId,
            DocumentTypeId=@DocumentTypeId, IsRequired=@IsRequired, AutoGenerate=@AutoGenerate, Separator=@Separator, IsActive=@IsActive, SortOrder=@SortOrder, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", convention) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE NamingConventions SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }

    public Task<string> GenerateNameAsync(Guid conventionId, Dictionary<string, string> values)
    {
        // This would need to be implemented with actual pattern replacement logic
        return Task.FromResult(string.Empty);
    }
}

public class OrganizationTemplateRepository : IOrganizationTemplateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrganizationTemplateRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<OrganizationTemplate>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<OrganizationTemplate>("SELECT * FROM OrganizationTemplates WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY Name", new { IncludeInactive = includeInactive });
    }

    public async Task<OrganizationTemplate?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<OrganizationTemplate>("SELECT * FROM OrganizationTemplates WHERE Id = @Id", new { Id = id });
    }

    public async Task<OrganizationTemplate?> GetDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<OrganizationTemplate>("SELECT * FROM OrganizationTemplates WHERE IsDefault = 1 AND IsActive = 1");
    }

    public async Task<Guid> CreateAsync(OrganizationTemplate template)
    {
        using var connection = _connectionFactory.CreateConnection();
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO OrganizationTemplates (Id, Name, Description, Structure, DefaultPermissions, IncludeContentTypes, IsDefault, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @Structure, @DefaultPermissions, @IncludeContentTypes, @IsDefault, @IsActive, @CreatedBy, @CreatedAt)", template);
        return template.Id;
    }

    public async Task<bool> UpdateAsync(OrganizationTemplate template)
    {
        using var connection = _connectionFactory.CreateConnection();
        template.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE OrganizationTemplates SET Name=@Name, Description=@Description, Structure=@Structure, DefaultPermissions=@DefaultPermissions,
            IncludeContentTypes=@IncludeContentTypes, IsDefault=@IsDefault, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", template) > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("UPDATE OrganizationTemplates SET IsDefault=0", transaction: transaction);
            await connection.ExecuteAsync("UPDATE OrganizationTemplates SET IsDefault=1 WHERE Id=@Id", new { Id = id }, transaction);
            transaction.Commit();
            return true;
        }
        catch { transaction.Rollback(); return false; }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE OrganizationTemplates SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class PermissionLevelDefinitionRepository : IPermissionLevelDefinitionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionLevelDefinitionRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<PermissionLevelDefinition>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionLevelDefinition>("SELECT * FROM PermissionLevelDefinitions WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY SortOrder, Level", new { IncludeInactive = includeInactive });
    }

    public async Task<PermissionLevelDefinition?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PermissionLevelDefinition>("SELECT * FROM PermissionLevelDefinitions WHERE Id = @Id", new { Id = id });
    }

    public async Task<PermissionLevelDefinition?> GetByLevelAsync(int level)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PermissionLevelDefinition>("SELECT * FROM PermissionLevelDefinitions WHERE Level = @Level AND IsActive = 1", new { Level = level });
    }

    public async Task<Guid> CreateAsync(PermissionLevelDefinition definition)
    {
        using var connection = _connectionFactory.CreateConnection();
        definition.Id = Guid.NewGuid();
        definition.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO PermissionLevelDefinitions (Id, Name, Description, Level, Color, Icon, CanRead, CanWrite, CanDelete, CanAdmin, CanShare, CanExport, IsSystem, IsActive, SortOrder, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @Level, @Color, @Icon, @CanRead, @CanWrite, @CanDelete, @CanAdmin, @CanShare, @CanExport, @IsSystem, @IsActive, @SortOrder, @CreatedBy, @CreatedAt)", definition);
        return definition.Id;
    }

    public async Task<bool> UpdateAsync(PermissionLevelDefinition definition)
    {
        using var connection = _connectionFactory.CreateConnection();
        definition.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE PermissionLevelDefinitions SET Name=@Name, Description=@Description, Level=@Level, Color=@Color, Icon=@Icon,
            CanRead=@CanRead, CanWrite=@CanWrite, CanDelete=@CanDelete, CanAdmin=@CanAdmin, CanShare=@CanShare, CanExport=@CanExport, IsSystem=@IsSystem, IsActive=@IsActive, SortOrder=@SortOrder, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", definition) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE PermissionLevelDefinitions SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id AND IsSystem=0", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class PurposeRepository : IPurposeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PurposeRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Purpose>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Purpose>("SELECT * FROM Purposes WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY SortOrder, Name", new { IncludeInactive = includeInactive });
    }

    public async Task<Purpose?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Purpose>("SELECT * FROM Purposes WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Purpose>> GetByTypeAsync(string purposeType)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Purpose>("SELECT * FROM Purposes WHERE PurposeType = @PurposeType AND IsActive = 1 ORDER BY SortOrder", new { PurposeType = purposeType });
    }

    public async Task<Purpose?> GetDefaultAsync(string purposeType)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Purpose>("SELECT * FROM Purposes WHERE PurposeType = @PurposeType AND IsDefault = 1 AND IsActive = 1", new { PurposeType = purposeType });
    }

    public async Task<Guid> CreateAsync(Purpose purpose)
    {
        using var connection = _connectionFactory.CreateConnection();
        purpose.Id = Guid.NewGuid();
        purpose.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO Purposes (Id, Name, Description, PurposeType, RequiresJustification, RequiresApproval, IsDefault, IsActive, SortOrder, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @PurposeType, @RequiresJustification, @RequiresApproval, @IsDefault, @IsActive, @SortOrder, @CreatedBy, @CreatedAt)", purpose);
        return purpose.Id;
    }

    public async Task<bool> UpdateAsync(Purpose purpose)
    {
        using var connection = _connectionFactory.CreateConnection();
        purpose.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE Purposes SET Name=@Name, Description=@Description, PurposeType=@PurposeType, RequiresJustification=@RequiresJustification,
            RequiresApproval=@RequiresApproval, IsDefault=@IsDefault, IsActive=@IsActive, SortOrder=@SortOrder, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", purpose) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE Purposes SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class ScanConfigRepository : IScanConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ScanConfigRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<ScanConfig>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ScanConfig>(@"SELECT sc.*, f.Name as TargetFolderName FROM ScanConfigs sc
            LEFT JOIN Folders f ON sc.TargetFolderId = f.Id WHERE (@IncludeInactive = 1 OR sc.IsActive = 1) ORDER BY sc.Name", new { IncludeInactive = includeInactive });
    }

    public async Task<ScanConfig?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ScanConfig>(@"SELECT sc.*, f.Name as TargetFolderName FROM ScanConfigs sc
            LEFT JOIN Folders f ON sc.TargetFolderId = f.Id WHERE sc.Id = @Id", new { Id = id });
    }

    public async Task<ScanConfig?> GetDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ScanConfig>("SELECT * FROM ScanConfigs WHERE IsDefault = 1 AND IsActive = 1");
    }

    public async Task<Guid> CreateAsync(ScanConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO ScanConfigs (Id, Name, Description, Resolution, ColorMode, OutputFormat, EnableOCR, OcrLanguage, AutoDeskew, AutoCrop, RemoveBlankPages, CompressionQuality, TargetFolderId, IsDefault, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @Resolution, @ColorMode, @OutputFormat, @EnableOCR, @OcrLanguage, @AutoDeskew, @AutoCrop, @RemoveBlankPages, @CompressionQuality, @TargetFolderId, @IsDefault, @IsActive, @CreatedBy, @CreatedAt)", config);
        return config.Id;
    }

    public async Task<bool> UpdateAsync(ScanConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE ScanConfigs SET Name=@Name, Description=@Description, Resolution=@Resolution, ColorMode=@ColorMode, OutputFormat=@OutputFormat,
            EnableOCR=@EnableOCR, OcrLanguage=@OcrLanguage, AutoDeskew=@AutoDeskew, AutoCrop=@AutoCrop, RemoveBlankPages=@RemoveBlankPages, CompressionQuality=@CompressionQuality,
            TargetFolderId=@TargetFolderId, IsDefault=@IsDefault, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", config) > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("UPDATE ScanConfigs SET IsDefault=0", transaction: transaction);
            await connection.ExecuteAsync("UPDATE ScanConfigs SET IsDefault=1 WHERE Id=@Id", new { Id = id }, transaction);
            transaction.Commit();
            return true;
        }
        catch { transaction.Rollback(); return false; }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE ScanConfigs SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

public class SearchConfigRepository : ISearchConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SearchConfigRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<SearchConfig>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SearchConfig>("SELECT * FROM SearchConfigs WHERE (@IncludeInactive = 1 OR IsActive = 1) ORDER BY Name", new { IncludeInactive = includeInactive });
    }

    public async Task<IEnumerable<SearchConfig>> GetGlobalAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SearchConfig>("SELECT * FROM SearchConfigs WHERE IsGlobal = 1 AND IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<SearchConfig>> GetByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SearchConfig>("SELECT * FROM SearchConfigs WHERE (IsGlobal = 1 OR CreatedBy = @UserId) AND IsActive = 1 ORDER BY Name", new { UserId = userId });
    }

    public async Task<SearchConfig?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SearchConfig>("SELECT * FROM SearchConfigs WHERE Id = @Id", new { Id = id });
    }

    public async Task<SearchConfig?> GetDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SearchConfig>("SELECT * FROM SearchConfigs WHERE IsDefault = 1 AND IsActive = 1");
    }

    public async Task<Guid> CreateAsync(SearchConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"INSERT INTO SearchConfigs (Id, Name, Description, SearchType, DefaultFields, Filters, IncludeContent, IncludeMetadata, IncludeVersions, FuzzyMatch, MaxResults, SortField, SortDirection, IsGlobal, IsDefault, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @SearchType, @DefaultFields, @Filters, @IncludeContent, @IncludeMetadata, @IncludeVersions, @FuzzyMatch, @MaxResults, @SortField, @SortDirection, @IsGlobal, @IsDefault, @IsActive, @CreatedBy, @CreatedAt)", config);
        return config.Id;
    }

    public async Task<bool> UpdateAsync(SearchConfig config)
    {
        using var connection = _connectionFactory.CreateConnection();
        config.ModifiedAt = DateTime.UtcNow;
        return await connection.ExecuteAsync(@"UPDATE SearchConfigs SET Name=@Name, Description=@Description, SearchType=@SearchType, DefaultFields=@DefaultFields, Filters=@Filters,
            IncludeContent=@IncludeContent, IncludeMetadata=@IncludeMetadata, IncludeVersions=@IncludeVersions, FuzzyMatch=@FuzzyMatch, MaxResults=@MaxResults,
            SortField=@SortField, SortDirection=@SortDirection, IsGlobal=@IsGlobal, IsDefault=@IsDefault, IsActive=@IsActive, ModifiedBy=@ModifiedBy, ModifiedAt=@ModifiedAt WHERE Id=@Id", config) > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync("UPDATE SearchConfigs SET IsDefault=0", transaction: transaction);
            await connection.ExecuteAsync("UPDATE SearchConfigs SET IsDefault=1 WHERE Id=@Id", new { Id = id }, transaction);
            transaction.Commit();
            return true;
        }
        catch { transaction.Rollback(); return false; }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("UPDATE SearchConfigs SET IsActive=0, ModifiedAt=@Now WHERE Id=@Id", new { Id = id, Now = DateTime.UtcNow }) > 0;
    }
}

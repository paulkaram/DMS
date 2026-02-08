using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IBookmarkRepository
{
    Task<IEnumerable<Bookmark>> GetAllAsync(bool includeInactive = false);
    Task<Bookmark?> GetByIdAsync(Guid id);
    Task<Bookmark?> GetByPlaceholderAsync(string placeholder);
    Task<Guid> CreateAsync(Bookmark bookmark);
    Task<bool> UpdateAsync(Bookmark bookmark);
    Task<bool> DeleteAsync(Guid id);
}

public interface ICaseRepository
{
    Task<IEnumerable<Case>> GetAllAsync(bool includeInactive = false);
    Task<Case?> GetByIdAsync(Guid id);
    Task<Case?> GetByCaseNumberAsync(string caseNumber);
    Task<IEnumerable<Case>> GetByStatusAsync(string status);
    Task<IEnumerable<Case>> GetByAssigneeAsync(Guid userId);
    Task<Guid> CreateAsync(Case caseEntity);
    Task<bool> UpdateAsync(Case caseEntity);
    Task<bool> UpdateStatusAsync(Guid id, string status, Guid userId);
    Task<bool> DeleteAsync(Guid id);
}

public interface IEndpointRepository
{
    Task<IEnumerable<ServiceEndpoint>> GetAllAsync(bool includeInactive = false);
    Task<ServiceEndpoint?> GetByIdAsync(Guid id);
    Task<IEnumerable<ServiceEndpoint>> GetByTypeAsync(string endpointType);
    Task<Guid> CreateAsync(ServiceEndpoint endpoint);
    Task<bool> UpdateAsync(ServiceEndpoint endpoint);
    Task<bool> UpdateHealthStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}

public interface IExportConfigRepository
{
    Task<IEnumerable<ExportConfig>> GetAllAsync(bool includeInactive = false);
    Task<ExportConfig?> GetByIdAsync(Guid id);
    Task<ExportConfig?> GetDefaultAsync();
    Task<Guid> CreateAsync(ExportConfig config);
    Task<bool> UpdateAsync(ExportConfig config);
    Task<bool> SetDefaultAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}

public interface INamingConventionRepository
{
    Task<IEnumerable<NamingConvention>> GetAllAsync(bool includeInactive = false);
    Task<NamingConvention?> GetByIdAsync(Guid id);
    Task<IEnumerable<NamingConvention>> GetByFolderAsync(Guid folderId);
    Task<NamingConvention?> GetApplicableAsync(Guid? folderId, Guid? documentTypeId, string appliesTo);
    Task<Guid> CreateAsync(NamingConvention convention);
    Task<bool> UpdateAsync(NamingConvention convention);
    Task<bool> DeleteAsync(Guid id);
    Task<string> GenerateNameAsync(Guid conventionId, Dictionary<string, string> values);
}

public interface IOrganizationTemplateRepository
{
    Task<IEnumerable<OrganizationTemplate>> GetAllAsync(bool includeInactive = false);
    Task<OrganizationTemplate?> GetByIdAsync(Guid id);
    Task<OrganizationTemplate?> GetDefaultAsync();
    Task<Guid> CreateAsync(OrganizationTemplate template);
    Task<bool> UpdateAsync(OrganizationTemplate template);
    Task<bool> SetDefaultAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}

public interface IPermissionLevelDefinitionRepository
{
    Task<IEnumerable<PermissionLevelDefinition>> GetAllAsync(bool includeInactive = false);
    Task<PermissionLevelDefinition?> GetByIdAsync(Guid id);
    Task<PermissionLevelDefinition?> GetByLevelAsync(int level);
    Task<Guid> CreateAsync(PermissionLevelDefinition definition);
    Task<bool> UpdateAsync(PermissionLevelDefinition definition);
    Task<bool> DeleteAsync(Guid id);
}

public interface IPurposeRepository
{
    Task<IEnumerable<Purpose>> GetAllAsync(bool includeInactive = false);
    Task<Purpose?> GetByIdAsync(Guid id);
    Task<IEnumerable<Purpose>> GetByTypeAsync(string purposeType);
    Task<Purpose?> GetDefaultAsync(string purposeType);
    Task<Guid> CreateAsync(Purpose purpose);
    Task<bool> UpdateAsync(Purpose purpose);
    Task<bool> DeleteAsync(Guid id);
}

public interface IScanConfigRepository
{
    Task<IEnumerable<ScanConfig>> GetAllAsync(bool includeInactive = false);
    Task<ScanConfig?> GetByIdAsync(Guid id);
    Task<ScanConfig?> GetDefaultAsync();
    Task<Guid> CreateAsync(ScanConfig config);
    Task<bool> UpdateAsync(ScanConfig config);
    Task<bool> SetDefaultAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}

public interface ISearchConfigRepository
{
    Task<IEnumerable<SearchConfig>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<SearchConfig>> GetGlobalAsync();
    Task<IEnumerable<SearchConfig>> GetByUserAsync(Guid userId);
    Task<SearchConfig?> GetByIdAsync(Guid id);
    Task<SearchConfig?> GetDefaultAsync();
    Task<Guid> CreateAsync(SearchConfig config);
    Task<bool> UpdateAsync(SearchConfig config);
    Task<bool> SetDefaultAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}

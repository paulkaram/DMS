using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface ILegalHoldRepository
{
    Task<LegalHold?> GetByIdAsync(Guid id);
    Task<IEnumerable<LegalHold>> GetAllAsync();
    Task<IEnumerable<LegalHold>> GetActiveAsync();
    Task<Guid> CreateAsync(LegalHold entity);
    Task<bool> UpdateAsync(LegalHold entity);
}

public interface ILegalHoldDocumentRepository
{
    Task<LegalHoldDocument?> GetByIdAsync(Guid id);
    Task<IEnumerable<LegalHoldDocument>> GetByHoldIdAsync(Guid holdId);
    Task<IEnumerable<LegalHoldDocument>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<LegalHoldDocument>> GetActiveByDocumentIdAsync(Guid documentId);
    Task<bool> IsDocumentOnHoldAsync(Guid documentId);
    Task<Guid> CreateAsync(LegalHoldDocument entity);
    Task<bool> UpdateAsync(LegalHoldDocument entity);
    Task<bool> DeleteAsync(Guid id);
}

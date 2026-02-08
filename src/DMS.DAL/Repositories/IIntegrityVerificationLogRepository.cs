using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IIntegrityVerificationLogRepository
{
    Task<IntegrityVerificationLog?> GetByIdAsync(Guid id);
    Task<IEnumerable<IntegrityVerificationLog>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<IntegrityVerificationLog>> GetFailuresAsync(DateTime? since = null);
    Task<Guid> CreateAsync(IntegrityVerificationLog entity);
}

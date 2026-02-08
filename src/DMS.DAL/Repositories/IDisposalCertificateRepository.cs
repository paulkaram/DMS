using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDisposalCertificateRepository
{
    Task<DisposalCertificate?> GetByIdAsync(Guid id);
    Task<DisposalCertificate?> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DisposalCertificate>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<Guid> CreateAsync(DisposalCertificate entity);
    Task<bool> UpdateAsync(DisposalCertificate entity);
    Task<string> GenerateCertificateNumberAsync();
}

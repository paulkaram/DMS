using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDisposalRequestRepository
{
    Task<DisposalRequest?> GetByIdAsync(Guid id);
    Task<DisposalRequest?> GetByIdWithDetailsAsync(Guid id);
    Task<(List<DisposalRequest> Items, int TotalCount)> GetPaginatedAsync(DisposalRequestStatus? status, int page, int pageSize);
    Task<Guid> CreateAsync(DisposalRequest request);
    Task UpdateAsync(DisposalRequest request);
    Task<string> GenerateRequestNumberAsync();
    Task AddDocumentsAsync(Guid requestId, IEnumerable<Guid> documentIds);
    Task<List<DisposalRequestDocument>> GetDocumentsAsync(Guid requestId);
    Task UpdateDocumentStatusAsync(Guid requestDocId, DisposalRequestDocumentStatus status, Guid? certificateId = null, string? error = null);
    Task AddApprovalAsync(DisposalApproval approval);
    Task<List<DisposalApproval>> GetApprovalsAsync(Guid requestId);
}

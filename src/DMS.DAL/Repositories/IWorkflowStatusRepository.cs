using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IWorkflowStatusRepository
{
    Task<WorkflowStatus?> GetByIdAsync(Guid id);
    Task<IEnumerable<WorkflowStatus>> GetAllAsync(bool includeInactive = false);
    Task<Guid> CreateAsync(WorkflowStatus entity);
    Task<bool> UpdateAsync(WorkflowStatus entity);
    Task<bool> DeleteAsync(Guid id);
}

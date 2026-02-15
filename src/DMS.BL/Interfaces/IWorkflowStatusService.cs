using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IWorkflowStatusService
{
    Task<ServiceResult<IEnumerable<WorkflowStatusDto>>> GetAllAsync(bool includeInactive = false);
    Task<ServiceResult<WorkflowStatusDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<WorkflowStatusDto>> CreateAsync(CreateWorkflowStatusRequest request);
    Task<ServiceResult<WorkflowStatusDto>> UpdateAsync(Guid id, UpdateWorkflowStatusRequest request);
    Task<ServiceResult> DeleteAsync(Guid id);
}

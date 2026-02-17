using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IPhysicalLocationService
{
    Task<ServiceResult<List<PhysicalLocationDto>>> GetTreeAsync();
    Task<ServiceResult<List<PhysicalLocationDto>>> GetChildrenAsync(Guid? parentId);
    Task<ServiceResult<PhysicalLocationDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PhysicalLocationDto>> CreateAsync(CreatePhysicalLocationDto dto, Guid userId);
    Task<ServiceResult<PhysicalLocationDto>> UpdateAsync(Guid id, CreatePhysicalLocationDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);
}

public interface IPhysicalItemService
{
    Task<ServiceResult<PhysicalItemDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PhysicalItemDto>> GetByBarcodeAsync(string barcode);
    Task<ServiceResult<PagedResultDto<PhysicalItemDto>>> SearchAsync(string? query, Guid? locationId, string? itemType, int page, int pageSize);
    Task<ServiceResult<PhysicalItemDto>> CreateAsync(CreatePhysicalItemDto dto, Guid userId);
    Task<ServiceResult<PhysicalItemDto>> UpdateAsync(Guid id, CreatePhysicalItemDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);
    Task<ServiceResult> MoveAsync(Guid id, MovePhysicalItemDto dto, Guid userId);
    Task<ServiceResult> UpdateConditionAsync(Guid id, UpdateConditionDto dto, Guid userId);
}

public interface IAccessionService
{
    Task<ServiceResult<AccessionRequestDto>> CreateRequestAsync(CreateAccessionRequestDto dto, Guid userId);
    Task<ServiceResult<AccessionRequestDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResultDto<AccessionRequestDto>>> GetPaginatedAsync(string? status, int page, int pageSize);
    Task<ServiceResult> SubmitAsync(Guid id, Guid userId);
    Task<ServiceResult> ReviewAsync(Guid id, ReviewAccessionDto dto, Guid userId);
    Task<ServiceResult> AcceptAsync(Guid id, ReviewAccessionDto dto, Guid userId);
    Task<ServiceResult> RejectAsync(Guid id, string reason, Guid userId);
    Task<ServiceResult> ExecuteTransferAsync(Guid id, Guid userId);
    Task<ServiceResult> AddItemAsync(Guid id, AddAccessionItemDto dto);
    Task<ServiceResult> RemoveItemAsync(Guid id, Guid itemId);
}

public interface ICirculationService
{
    Task<ServiceResult<CirculationRecordDto>> CheckOutAsync(CheckOutPhysicalItemDto dto, Guid userId);
    Task<ServiceResult> ReturnAsync(Guid circulationId, ReturnPhysicalItemDto dto, Guid userId);
    Task<ServiceResult> RenewAsync(Guid circulationId, Guid userId);
    Task<ServiceResult<List<CirculationRecordDto>>> GetActiveLoansAsync();
    Task<ServiceResult<List<CirculationRecordDto>>> GetOverdueAsync();
    Task<ServiceResult<List<CirculationRecordDto>>> GetItemHistoryAsync(Guid physicalItemId);
    Task<ServiceResult> ReportLostAsync(Guid circulationId, Guid userId);
}

public interface ICustodyService
{
    Task<ServiceResult<CustodyTransferDto>> RecordTransferAsync(Guid physicalItemId, CreateCustodyTransferDto dto, Guid userId);
    Task<ServiceResult> AcknowledgeTransferAsync(Guid transferId, Guid userId);
    Task<ServiceResult<List<CustodyTransferDto>>> GetChainOfCustodyAsync(Guid physicalItemId);
}

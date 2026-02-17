using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IPhysicalLocationRepository
{
    Task<PhysicalLocation?> GetByIdAsync(Guid id);
    Task<List<PhysicalLocation>> GetChildrenAsync(Guid? parentId);
    Task<List<PhysicalLocation>> GetAllAsync();
    Task<Guid> CreateAsync(PhysicalLocation location);
    Task UpdateAsync(PhysicalLocation location);
    Task DeleteAsync(Guid id);
}

public interface IPhysicalItemRepository
{
    Task<PhysicalItem?> GetByIdAsync(Guid id);
    Task<PhysicalItem?> GetByBarcodeAsync(string barcode);
    Task<(List<PhysicalItem> Items, int TotalCount)> SearchAsync(string? query, Guid? locationId, PhysicalItemType? itemType, int page, int pageSize);
    Task<Guid> CreateAsync(PhysicalItem item);
    Task UpdateAsync(PhysicalItem item);
    Task DeleteAsync(Guid id);
}

public interface IAccessionRequestRepository
{
    Task<AccessionRequest?> GetByIdAsync(Guid id);
    Task<(List<AccessionRequest> Items, int TotalCount)> GetPaginatedAsync(AccessionStatus? status, int page, int pageSize);
    Task<Guid> CreateAsync(AccessionRequest request);
    Task UpdateAsync(AccessionRequest request);
    Task<string> GenerateAccessionNumberAsync();
    Task<List<AccessionRequestItem>> GetItemsAsync(Guid requestId);
    Task AddItemAsync(AccessionRequestItem item);
    Task RemoveItemAsync(Guid itemId);
}

public interface ICirculationRecordRepository
{
    Task<CirculationRecord?> GetByIdAsync(Guid id);
    Task<List<CirculationRecord>> GetActiveLoansAsync();
    Task<List<CirculationRecord>> GetOverdueAsync();
    Task<List<CirculationRecord>> GetByItemIdAsync(Guid physicalItemId);
    Task<(List<CirculationRecord> Items, int TotalCount)> GetPaginatedAsync(CirculationRecordStatus? status, int page, int pageSize);
    Task<Guid> CreateAsync(CirculationRecord record);
    Task UpdateAsync(CirculationRecord record);
}

public interface ICustodyTransferRepository
{
    Task<List<CustodyTransfer>> GetByItemIdAsync(Guid physicalItemId);
    Task<CustodyTransfer?> GetByIdAsync(Guid id);
    Task<CustodyTransfer?> GetLatestByItemIdAsync(Guid physicalItemId);
    Task<Guid> CreateAsync(CustodyTransfer transfer);
    Task UpdateAsync(CustodyTransfer transfer);
}

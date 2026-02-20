using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class PhysicalLocationRepository : IPhysicalLocationRepository
{
    private readonly DmsDbContext _context;
    public PhysicalLocationRepository(DmsDbContext context) => _context = context;

    public async Task<PhysicalLocation?> GetByIdAsync(Guid id) => await _context.PhysicalLocations.FindAsync(id);

    public async Task<List<PhysicalLocation>> GetChildrenAsync(Guid? parentId)
    {
        return await _context.PhysicalLocations
            .Where(l => l.ParentId == parentId)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<List<PhysicalLocation>> GetAllAsync()
    {
        return await _context.PhysicalLocations.OrderBy(l => l.Path).ToListAsync();
    }

    public async Task<Guid> CreateAsync(PhysicalLocation location)
    {
        location.Id = Guid.NewGuid();
        _context.PhysicalLocations.Add(location);
        await _context.SaveChangesAsync();
        return location.Id;
    }

    public async Task UpdateAsync(PhysicalLocation location)
    {
        _context.PhysicalLocations.Update(location);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.PhysicalLocations.FindAsync(id);
        if (entity != null)
        {
            entity.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetItemCountAsync(Guid locationId)
    {
        return await _context.PhysicalItems.CountAsync(i => i.LocationId == locationId && i.IsActive);
    }

    public async Task<List<Guid>> GetAllDescendantIdsAsync(Guid locationId)
    {
        var all = await _context.PhysicalLocations
            .Where(l => l.IsActive)
            .Select(l => new { l.Id, l.ParentId })
            .ToListAsync();

        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(locationId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in all.Where(l => l.ParentId == current))
            {
                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    public async Task<int> GetItemCountForLocationsAsync(IEnumerable<Guid> locationIds)
    {
        var ids = locationIds.ToList();
        return await _context.PhysicalItems.CountAsync(i => i.LocationId.HasValue && ids.Contains(i.LocationId.Value) && i.IsActive);
    }

    public async Task<int> GetChildCapacitySumAsync(Guid parentId, Guid? excludeId = null)
    {
        var q = _context.PhysicalLocations.Where(l => l.ParentId == parentId && l.IsActive);
        if (excludeId.HasValue)
            q = q.Where(l => l.Id != excludeId.Value);
        return await q.SumAsync(l => l.Capacity ?? 0);
    }
}

public class PhysicalItemRepository : IPhysicalItemRepository
{
    private readonly DmsDbContext _context;
    public PhysicalItemRepository(DmsDbContext context) => _context = context;

    public async Task<PhysicalItem?> GetByIdAsync(Guid id) => await _context.PhysicalItems.FindAsync(id);

    public async Task<PhysicalItem?> GetByBarcodeAsync(string barcode)
    {
        return await _context.PhysicalItems.FirstOrDefaultAsync(i => i.Barcode == barcode);
    }

    public async Task<(List<PhysicalItem> Items, int TotalCount)> SearchAsync(
        string? query, Guid? locationId, PhysicalItemType? itemType, int page, int pageSize)
    {
        var q = _context.PhysicalItems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(i => i.Title.Contains(query) || i.Barcode.Contains(query) || (i.Description != null && i.Description.Contains(query)));
        if (locationId.HasValue)
            q = q.Where(i => i.LocationId == locationId.Value);
        if (itemType.HasValue)
            q = q.Where(i => i.ItemType == itemType.Value);

        var totalCount = await q.CountAsync();
        var items = await q.OrderBy(i => i.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Guid> CreateAsync(PhysicalItem item)
    {
        item.Id = Guid.NewGuid();
        _context.PhysicalItems.Add(item);
        await _context.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateAsync(PhysicalItem item)
    {
        _context.PhysicalItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.PhysicalItems.FindAsync(id);
        if (entity != null)
        {
            entity.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}

public class AccessionRequestRepository : IAccessionRequestRepository
{
    private readonly DmsDbContext _context;
    public AccessionRequestRepository(DmsDbContext context) => _context = context;

    public async Task<AccessionRequest?> GetByIdAsync(Guid id) => await _context.AccessionRequests.FindAsync(id);

    public async Task<(List<AccessionRequest> Items, int TotalCount)> GetPaginatedAsync(
        AccessionStatus? status, int page, int pageSize)
    {
        var q = _context.AccessionRequests.AsQueryable();
        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);
        var totalCount = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Guid> CreateAsync(AccessionRequest request)
    {
        request.Id = Guid.NewGuid();
        _context.AccessionRequests.Add(request);
        await _context.SaveChangesAsync();
        return request.Id;
    }

    public async Task UpdateAsync(AccessionRequest request)
    {
        _context.AccessionRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateAccessionNumberAsync()
    {
        var year = DateTime.Now.Year;
        var count = await _context.AccessionRequests.CountAsync(r => r.CreatedAt.Year == year);
        return $"ACC-{year}-{(count + 1):D4}";
    }

    public async Task<List<AccessionRequestItem>> GetItemsAsync(Guid requestId)
    {
        return await _context.AccessionRequestItems.Where(i => i.AccessionRequestId == requestId).ToListAsync();
    }

    public async Task AddItemAsync(AccessionRequestItem item)
    {
        item.Id = Guid.NewGuid();
        _context.AccessionRequestItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(Guid itemId)
    {
        var item = await _context.AccessionRequestItems.FindAsync(itemId);
        if (item != null)
        {
            _context.AccessionRequestItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}

public class CirculationRecordRepository : ICirculationRecordRepository
{
    private readonly DmsDbContext _context;
    public CirculationRecordRepository(DmsDbContext context) => _context = context;

    public async Task<CirculationRecord?> GetByIdAsync(Guid id) => await _context.CirculationRecords.FindAsync(id);

    public async Task<List<CirculationRecord>> GetActiveLoansAsync()
    {
        return await _context.CirculationRecords
            .Where(r => r.Status == CirculationRecordStatus.Active || r.Status == CirculationRecordStatus.Overdue)
            .OrderBy(r => r.DueDate)
            .ToListAsync();
    }

    public async Task<List<CirculationRecord>> GetOverdueAsync()
    {
        return await _context.CirculationRecords
            .Where(r => r.Status == CirculationRecordStatus.Active && r.DueDate < DateTime.Now)
            .ToListAsync();
    }

    public async Task<List<CirculationRecord>> GetByItemIdAsync(Guid physicalItemId)
    {
        return await _context.CirculationRecords
            .Where(r => r.PhysicalItemId == physicalItemId)
            .OrderByDescending(r => r.CheckedOutAt)
            .ToListAsync();
    }

    public async Task<(List<CirculationRecord> Items, int TotalCount)> GetPaginatedAsync(
        CirculationRecordStatus? status, int page, int pageSize)
    {
        var q = _context.CirculationRecords.AsQueryable();
        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);
        var totalCount = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CheckedOutAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Guid> CreateAsync(CirculationRecord record)
    {
        record.Id = Guid.NewGuid();
        _context.CirculationRecords.Add(record);
        await _context.SaveChangesAsync();
        return record.Id;
    }

    public async Task UpdateAsync(CirculationRecord record)
    {
        _context.CirculationRecords.Update(record);
        await _context.SaveChangesAsync();
    }
}

public class CustodyTransferRepository : ICustodyTransferRepository
{
    private readonly DmsDbContext _context;
    public CustodyTransferRepository(DmsDbContext context) => _context = context;

    public async Task<List<CustodyTransfer>> GetByItemIdAsync(Guid physicalItemId)
    {
        return await _context.CustodyTransfers
            .Where(t => t.PhysicalItemId == physicalItemId)
            .OrderByDescending(t => t.TransferredAt)
            .ToListAsync();
    }

    public async Task<CustodyTransfer?> GetByIdAsync(Guid id) => await _context.CustodyTransfers.FindAsync(id);

    public async Task<CustodyTransfer?> GetLatestByItemIdAsync(Guid physicalItemId)
    {
        return await _context.CustodyTransfers
            .Where(t => t.PhysicalItemId == physicalItemId)
            .OrderByDescending(t => t.TransferredAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(CustodyTransfer transfer)
    {
        transfer.Id = Guid.NewGuid();
        _context.CustodyTransfers.Add(transfer);
        await _context.SaveChangesAsync();
        return transfer.Id;
    }

    public async Task UpdateAsync(CustodyTransfer transfer)
    {
        _context.CustodyTransfers.Update(transfer);
        await _context.SaveChangesAsync();
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class PhysicalLocationService : IPhysicalLocationService
{
    private readonly IPhysicalLocationRepository _repo;
    private readonly IActivityLogService _activityLog;

    public PhysicalLocationService(IPhysicalLocationRepository repo, IActivityLogService activityLog)
    {
        _repo = repo;
        _activityLog = activityLog;
    }

    public async Task<ServiceResult<List<PhysicalLocationDto>>> GetTreeAsync()
    {
        var all = await _repo.GetAllAsync();
        var tree = BuildTree(all, null);
        return ServiceResult<List<PhysicalLocationDto>>.Ok(tree);
    }

    public async Task<ServiceResult<List<PhysicalLocationDto>>> GetChildrenAsync(Guid? parentId)
    {
        var children = await _repo.GetChildrenAsync(parentId);
        return ServiceResult<List<PhysicalLocationDto>>.Ok(children.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<PhysicalLocationDto>> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<PhysicalLocationDto>.Fail("Location not found");
        return ServiceResult<PhysicalLocationDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult<PhysicalLocationDto>> CreateAsync(CreatePhysicalLocationDto dto, Guid userId)
    {
        if (!Enum.TryParse<LocationType>(dto.LocationType, true, out var locType))
            return ServiceResult<PhysicalLocationDto>.Fail("Invalid location type");

        // Validate child capacity doesn't exceed parent's capacity
        PhysicalLocation? parent = null;
        if (dto.ParentId.HasValue)
        {
            parent = await _repo.GetByIdAsync(dto.ParentId.Value);
            if (parent?.Capacity != null && dto.Capacity.HasValue)
            {
                var siblingCapacitySum = await _repo.GetChildCapacitySumAsync(dto.ParentId.Value);
                var remaining = parent.Capacity.Value - siblingCapacitySum;
                if (dto.Capacity.Value > remaining)
                    return ServiceResult<PhysicalLocationDto>.Fail(
                        $"Cannot allocate capacity {dto.Capacity.Value:N0} — parent \"{parent.Name}\" has {remaining:N0} remaining ({siblingCapacitySum:N0} of {parent.Capacity.Value:N0} already allocated)");
            }
        }

        var entity = new PhysicalLocation
        {
            ParentId = dto.ParentId,
            Name = dto.Name,
            NameAr = dto.NameAr,
            Code = dto.Code,
            LocationType = locType,
            Capacity = dto.Capacity,
            EnvironmentalConditions = dto.EnvironmentalConditions,
            Coordinates = dto.Coordinates,
            SecurityLevel = dto.SecurityLevel,
            SortOrder = dto.SortOrder,
            CreatedBy = userId
        };

        // Build path
        if (parent != null)
        {
            entity.Path = $"{parent.Path}/{dto.Code}";
            entity.Level = parent.Level + 1;
        }
        else if (dto.ParentId.HasValue)
        {
            entity.Path = dto.Code;
            entity.Level = 0;
        }
        else
        {
            entity.Path = dto.Code;
            entity.Level = 0;
        }

        var id = await _repo.CreateAsync(entity);
        entity.Id = id;

        await _activityLog.LogActivityAsync("Created", "PhysicalLocation", id, dto.Name, null, userId, null, null);
        return ServiceResult<PhysicalLocationDto>.Ok(MapToDto(entity), "Location created");
    }

    public async Task<ServiceResult<PhysicalLocationDto>> UpdateAsync(Guid id, CreatePhysicalLocationDto dto, Guid userId)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<PhysicalLocationDto>.Fail("Location not found");

        // Resolve new parent (may be different from current)
        var newParentId = dto.ParentId;
        PhysicalLocation? newParent = newParentId.HasValue ? await _repo.GetByIdAsync(newParentId.Value) : null;

        // Validate capacity against the NEW parent
        if (newParentId.HasValue && dto.Capacity.HasValue && newParent?.Capacity != null)
        {
            var siblingCapacitySum = await _repo.GetChildCapacitySumAsync(newParentId.Value, id);
            var remaining = newParent.Capacity.Value - siblingCapacitySum;
            if (dto.Capacity.Value > remaining)
                return ServiceResult<PhysicalLocationDto>.Fail(
                    $"Cannot set capacity to {dto.Capacity.Value:N0} — parent \"{newParent.Name}\" has {remaining:N0} remaining ({siblingCapacitySum:N0} of {newParent.Capacity.Value:N0} allocated to siblings)");
        }

        entity.Name = dto.Name;
        entity.NameAr = dto.NameAr;
        entity.Code = dto.Code;
        entity.ParentId = newParentId;
        entity.Capacity = dto.Capacity;
        entity.EnvironmentalConditions = dto.EnvironmentalConditions;
        entity.Coordinates = dto.Coordinates;
        entity.SecurityLevel = dto.SecurityLevel;
        entity.SortOrder = dto.SortOrder;
        entity.ModifiedBy = userId;

        // Rebuild path and level based on new parent
        if (newParent != null)
        {
            entity.Path = $"{newParent.Path}/{dto.Code}";
            entity.Level = newParent.Level + 1;
        }
        else
        {
            entity.Path = dto.Code;
            entity.Level = 0;
        }

        await _repo.UpdateAsync(entity);
        return ServiceResult<PhysicalLocationDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        await _repo.DeleteAsync(id);
        return ServiceResult.Ok("Location deleted");
    }

    private List<PhysicalLocationDto> BuildTree(List<PhysicalLocation> all, Guid? parentId)
    {
        return all.Where(l => l.ParentId == parentId)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .Select(l =>
            {
                var dto = MapToDto(l);
                dto.Children = BuildTree(all, l.Id);
                return dto;
            }).ToList();
    }

    private static PhysicalLocationDto MapToDto(PhysicalLocation e) => new()
    {
        Id = e.Id, ParentId = e.ParentId, Name = e.Name, NameAr = e.NameAr,
        Code = e.Code, LocationType = e.LocationType.ToString(), Path = e.Path,
        Level = e.Level, Capacity = e.Capacity, CurrentCount = e.CurrentCount,
        EnvironmentalConditions = e.EnvironmentalConditions, Coordinates = e.Coordinates,
        SecurityLevel = e.SecurityLevel, SortOrder = e.SortOrder
    };
}

public class PhysicalItemService : IPhysicalItemService
{
    private readonly IPhysicalItemRepository _repo;
    private readonly IPhysicalLocationRepository _locationRepo;
    private readonly IActivityLogService _activityLog;

    public PhysicalItemService(IPhysicalItemRepository repo, IPhysicalLocationRepository locationRepo, IActivityLogService activityLog)
    {
        _repo = repo;
        _locationRepo = locationRepo;
        _activityLog = activityLog;
    }

    private async Task<string?> ValidateCapacityAsync(Guid locationId)
    {
        // Walk up the ancestor chain, checking capacity at each level
        var currentId = (Guid?)locationId;
        while (currentId.HasValue)
        {
            var location = await _locationRepo.GetByIdAsync(currentId.Value);
            if (location == null) return currentId == locationId ? "Target location not found" : null;

            if (location.Capacity.HasValue)
            {
                // Count items at this location + all descendants
                var descendantIds = await _locationRepo.GetAllDescendantIdsAsync(currentId.Value);
                var allIds = new List<Guid> { currentId.Value };
                allIds.AddRange(descendantIds);
                var count = await _locationRepo.GetItemCountForLocationsAsync(allIds);

                if (count >= location.Capacity.Value)
                    return $"Location \"{location.Name}\" is at full capacity ({count:N0}/{location.Capacity.Value:N0})";
            }

            currentId = location.ParentId;
        }
        return null;
    }

    private async Task RefreshLocationCountAsync(Guid locationId)
    {
        // Refresh this location and all ancestors with recursive descendant counts
        var currentId = (Guid?)locationId;
        while (currentId.HasValue)
        {
            var location = await _locationRepo.GetByIdAsync(currentId.Value);
            if (location == null) break;

            var descendantIds = await _locationRepo.GetAllDescendantIdsAsync(currentId.Value);
            var allIds = new List<Guid> { currentId.Value };
            allIds.AddRange(descendantIds);
            location.CurrentCount = await _locationRepo.GetItemCountForLocationsAsync(allIds);
            await _locationRepo.UpdateAsync(location);

            currentId = location.ParentId;
        }
    }

    public async Task<ServiceResult<PhysicalItemDto>> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<PhysicalItemDto>.Fail("Physical item not found");
        return ServiceResult<PhysicalItemDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult<PhysicalItemDto>> GetByBarcodeAsync(string barcode)
    {
        var entity = await _repo.GetByBarcodeAsync(barcode);
        if (entity == null) return ServiceResult<PhysicalItemDto>.Fail("Physical item not found");
        return ServiceResult<PhysicalItemDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult<PagedResultDto<PhysicalItemDto>>> SearchAsync(
        string? query, Guid? locationId, string? itemType, int page, int pageSize)
    {
        PhysicalItemType? parsedType = null;
        if (!string.IsNullOrEmpty(itemType) && Enum.TryParse<PhysicalItemType>(itemType, true, out var t))
            parsedType = t;

        var (items, totalCount) = await _repo.SearchAsync(query, locationId, parsedType, page, pageSize);
        return ServiceResult<PagedResultDto<PhysicalItemDto>>.Ok(new PagedResultDto<PhysicalItemDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<PhysicalItemDto>> CreateAsync(CreatePhysicalItemDto dto, Guid userId)
    {
        if (!Enum.TryParse<PhysicalItemType>(dto.ItemType, true, out var itemType))
            return ServiceResult<PhysicalItemDto>.Fail("Invalid item type");
        if (!Enum.TryParse<ItemCondition>(dto.Condition, true, out var condition))
            condition = ItemCondition.Good;

        // Validate capacity at target location
        if (dto.LocationId.HasValue)
        {
            var capacityError = await ValidateCapacityAsync(dto.LocationId.Value);
            if (capacityError != null) return ServiceResult<PhysicalItemDto>.Fail(capacityError);
        }

        var barcode = GenerateBarcode();

        var entity = new PhysicalItem
        {
            Barcode = barcode,
            Title = dto.Title,
            TitleAr = dto.TitleAr,
            Description = dto.Description,
            ItemType = itemType,
            LocationId = dto.LocationId,
            DigitalDocumentId = dto.DigitalDocumentId,
            ClassificationId = dto.ClassificationId,
            RetentionPolicyId = dto.RetentionPolicyId,
            Condition = condition,
            ItemDate = dto.ItemDate,
            DateRangeStart = dto.DateRangeStart,
            DateRangeEnd = dto.DateRangeEnd,
            PageCount = dto.PageCount,
            Dimensions = dto.Dimensions,
            CreatedBy = userId
        };

        var id = await _repo.CreateAsync(entity);
        entity.Id = id;

        // Refresh location count
        if (dto.LocationId.HasValue)
            await RefreshLocationCountAsync(dto.LocationId.Value);

        await _activityLog.LogActivityAsync("Created", "PhysicalItem", id, dto.Title, null, userId, null, null);
        return ServiceResult<PhysicalItemDto>.Ok(MapToDto(entity), "Physical item created");
    }

    public async Task<ServiceResult<PhysicalItemDto>> UpdateAsync(Guid id, CreatePhysicalItemDto dto, Guid userId)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<PhysicalItemDto>.Fail("Physical item not found");

        var oldLocationId = entity.LocationId;
        var locationChanged = oldLocationId != dto.LocationId;

        // Validate capacity at new location if changed
        if (locationChanged && dto.LocationId.HasValue)
        {
            var capacityError = await ValidateCapacityAsync(dto.LocationId.Value);
            if (capacityError != null) return ServiceResult<PhysicalItemDto>.Fail(capacityError);
        }

        entity.Title = dto.Title;
        entity.TitleAr = dto.TitleAr;
        entity.Description = dto.Description;
        entity.LocationId = dto.LocationId;
        entity.DigitalDocumentId = dto.DigitalDocumentId;
        entity.ClassificationId = dto.ClassificationId;
        entity.RetentionPolicyId = dto.RetentionPolicyId;
        entity.ItemDate = dto.ItemDate;
        entity.DateRangeStart = dto.DateRangeStart;
        entity.DateRangeEnd = dto.DateRangeEnd;
        entity.PageCount = dto.PageCount;
        entity.Dimensions = dto.Dimensions;
        entity.ModifiedBy = userId;

        await _repo.UpdateAsync(entity);

        // Refresh counts for both old and new locations
        if (locationChanged)
        {
            if (oldLocationId.HasValue) await RefreshLocationCountAsync(oldLocationId.Value);
            if (dto.LocationId.HasValue) await RefreshLocationCountAsync(dto.LocationId.Value);
        }

        return ServiceResult<PhysicalItemDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult.Fail("Physical item not found");
        if (entity.CirculationStatus == CirculationStatus.CheckedOut)
            return ServiceResult.Fail("Cannot delete a checked-out item");
        if (entity.IsOnLegalHold)
            return ServiceResult.Fail("Cannot delete an item under legal hold");

        var locationId = entity.LocationId;
        await _repo.DeleteAsync(id);

        // Refresh location count
        if (locationId.HasValue)
            await RefreshLocationCountAsync(locationId.Value);

        return ServiceResult.Ok("Physical item deleted");
    }

    public async Task<ServiceResult> MoveAsync(Guid id, MovePhysicalItemDto dto, Guid userId)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult.Fail("Physical item not found");

        // Validate capacity at destination
        var capacityError = await ValidateCapacityAsync(dto.NewLocationId);
        if (capacityError != null) return ServiceResult.Fail(capacityError);

        var oldLocationId = entity.LocationId;
        entity.LocationId = dto.NewLocationId;
        entity.ModifiedBy = userId;
        await _repo.UpdateAsync(entity);

        // Refresh counts for both locations
        if (oldLocationId.HasValue) await RefreshLocationCountAsync(oldLocationId.Value);
        await RefreshLocationCountAsync(dto.NewLocationId);

        await _activityLog.LogActivityAsync("Moved", "PhysicalItem", id, entity.Title,
            $"Moved to location {dto.NewLocationId}", userId, null, null);
        return ServiceResult.Ok("Item moved");
    }

    public async Task<ServiceResult> UpdateConditionAsync(Guid id, UpdateConditionDto dto, Guid userId)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return ServiceResult.Fail("Physical item not found");

        if (Enum.TryParse<ItemCondition>(dto.Condition, true, out var condition))
            entity.Condition = condition;
        entity.ConditionNotes = dto.Notes;
        entity.LastConditionAssessment = DateTime.Now;
        entity.ModifiedBy = userId;
        await _repo.UpdateAsync(entity);
        return ServiceResult.Ok("Condition updated");
    }

    private static string GenerateBarcode()
    {
        var segment = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"PHY-{DateTime.Now:yyyyMMdd}-{segment}";
    }

    private static PhysicalItemDto MapToDto(PhysicalItem e) => new()
    {
        Id = e.Id, Barcode = e.Barcode, BarcodeFormat = e.BarcodeFormat,
        Title = e.Title, TitleAr = e.TitleAr, Description = e.Description,
        ItemType = e.ItemType.ToString(), LocationId = e.LocationId,
        DigitalDocumentId = e.DigitalDocumentId, ClassificationId = e.ClassificationId,
        RetentionPolicyId = e.RetentionPolicyId, Condition = e.Condition.ToString(),
        ItemDate = e.ItemDate, DateRangeStart = e.DateRangeStart, DateRangeEnd = e.DateRangeEnd,
        PageCount = e.PageCount, Dimensions = e.Dimensions,
        CirculationStatus = e.CirculationStatus.ToString(), IsOnLegalHold = e.IsOnLegalHold,
        LastConditionAssessment = e.LastConditionAssessment, ConditionNotes = e.ConditionNotes,
        CreatedAt = e.CreatedAt
    };
}

public class AccessionService : IAccessionService
{
    private readonly IAccessionRequestRepository _repo;
    private readonly IActivityLogService _activityLog;

    public AccessionService(IAccessionRequestRepository repo, IActivityLogService activityLog)
    {
        _repo = repo;
        _activityLog = activityLog;
    }

    public async Task<ServiceResult<AccessionRequestDto>> CreateRequestAsync(CreateAccessionRequestDto dto, Guid userId)
    {
        var request = new AccessionRequest
        {
            AccessionNumber = await _repo.GenerateAccessionNumberAsync(),
            SourceStructureId = dto.SourceStructureId,
            TargetLocationId = dto.TargetLocationId,
            RecordsDateFrom = dto.RecordsDateFrom,
            RecordsDateTo = dto.RecordsDateTo,
            RequestedTransferDate = dto.RequestedTransferDate,
            Status = AccessionStatus.Draft,
            CreatedBy = userId
        };
        var id = await _repo.CreateAsync(request);
        request.Id = id;
        return ServiceResult<AccessionRequestDto>.Ok(MapToDto(request), "Accession request created");
    }

    public async Task<ServiceResult<AccessionRequestDto>> GetByIdAsync(Guid id)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult<AccessionRequestDto>.Fail("Accession request not found");
        var dto = MapToDto(request);
        var items = await _repo.GetItemsAsync(id);
        dto.Items = items.Select(i => new AccessionRequestItemDto
        {
            Id = i.Id, PhysicalItemId = i.PhysicalItemId, Title = i.Title,
            ItemType = i.ItemType.ToString(), Status = i.Status, Notes = i.Notes
        }).ToList();
        dto.ItemCount = dto.Items.Count;
        return ServiceResult<AccessionRequestDto>.Ok(dto);
    }

    public async Task<ServiceResult<PagedResultDto<AccessionRequestDto>>> GetPaginatedAsync(string? status, int page, int pageSize)
    {
        AccessionStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AccessionStatus>(status, true, out var parsed))
            statusFilter = parsed;
        var (items, totalCount) = await _repo.GetPaginatedAsync(statusFilter, page, pageSize);
        return ServiceResult<PagedResultDto<AccessionRequestDto>>.Ok(new PagedResultDto<AccessionRequestDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount, PageNumber = page, PageSize = pageSize
        });
    }

    public async Task<ServiceResult> SubmitAsync(Guid id, Guid userId)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult.Fail("Not found");
        if (request.Status != AccessionStatus.Draft) return ServiceResult.Fail("Can only submit from Draft status");
        request.Status = AccessionStatus.Submitted;
        request.ModifiedBy = userId;
        await _repo.UpdateAsync(request);
        return ServiceResult.Ok("Submitted for review");
    }

    public async Task<ServiceResult> ReviewAsync(Guid id, ReviewAccessionDto dto, Guid userId)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult.Fail("Not found");
        request.Status = AccessionStatus.UnderReview;
        request.ReviewedBy = userId;
        request.ReviewedAt = DateTime.Now;
        request.ReviewNotes = dto.Notes;
        request.ModifiedBy = userId;
        await _repo.UpdateAsync(request);
        return ServiceResult.Ok("Under review");
    }

    public async Task<ServiceResult> AcceptAsync(Guid id, ReviewAccessionDto dto, Guid userId)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult.Fail("Not found");
        request.Status = AccessionStatus.Accepted;
        request.AcceptedBy = userId;
        request.AcceptedAt = DateTime.Now;
        request.AcceptanceNotes = dto.Notes;
        request.ModifiedBy = userId;
        await _repo.UpdateAsync(request);
        return ServiceResult.Ok("Accession accepted");
    }

    public async Task<ServiceResult> RejectAsync(Guid id, string reason, Guid userId)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult.Fail("Not found");
        request.Status = AccessionStatus.Rejected;
        request.RejectionReason = reason;
        request.ModifiedBy = userId;
        await _repo.UpdateAsync(request);
        return ServiceResult.Ok("Accession rejected");
    }

    public async Task<ServiceResult> ExecuteTransferAsync(Guid id, Guid userId)
    {
        var request = await _repo.GetByIdAsync(id);
        if (request == null) return ServiceResult.Fail("Not found");
        if (request.Status != AccessionStatus.Accepted)
            return ServiceResult.Fail("Must be accepted before transfer");
        request.Status = AccessionStatus.Transferred;
        request.ActualTransferDate = DateTime.Now;
        request.ModifiedBy = userId;
        await _repo.UpdateAsync(request);
        await _activityLog.LogActivityAsync("AccessionTransferred", "AccessionRequest", id,
            request.AccessionNumber, null, userId, null, null);
        return ServiceResult.Ok("Transfer executed");
    }

    public async Task<ServiceResult> AddItemAsync(Guid id, AddAccessionItemDto dto)
    {
        if (!Enum.TryParse<PhysicalItemType>(dto.ItemType, true, out var itemType))
            return ServiceResult.Fail("Invalid item type");
        await _repo.AddItemAsync(new AccessionRequestItem
        {
            AccessionRequestId = id, PhysicalItemId = dto.PhysicalItemId,
            Title = dto.Title, ItemType = itemType, ClassificationId = dto.ClassificationId, Notes = dto.Notes
        });
        return ServiceResult.Ok("Item added");
    }

    public async Task<ServiceResult> RemoveItemAsync(Guid id, Guid itemId)
    {
        await _repo.RemoveItemAsync(itemId);
        return ServiceResult.Ok("Item removed");
    }

    private static AccessionRequestDto MapToDto(AccessionRequest e) => new()
    {
        Id = e.Id, AccessionNumber = e.AccessionNumber, SourceStructureId = e.SourceStructureId,
        TargetLocationId = e.TargetLocationId, Status = e.Status.ToString(), ItemCount = e.ItemCount,
        RecordsDateFrom = e.RecordsDateFrom, RecordsDateTo = e.RecordsDateTo,
        RequestedTransferDate = e.RequestedTransferDate, ActualTransferDate = e.ActualTransferDate,
        CreatedAt = e.CreatedAt
    };
}

public class CirculationService : ICirculationService
{
    private readonly ICirculationRecordRepository _repo;
    private readonly IPhysicalItemRepository _itemRepo;
    private readonly ICustodyService _custodyService;
    private readonly IActivityLogService _activityLog;

    public CirculationService(ICirculationRecordRepository repo, IPhysicalItemRepository itemRepo,
        ICustodyService custodyService, IActivityLogService activityLog)
    {
        _repo = repo;
        _itemRepo = itemRepo;
        _custodyService = custodyService;
        _activityLog = activityLog;
    }

    public async Task<ServiceResult<CirculationRecordDto>> CheckOutAsync(CheckOutPhysicalItemDto dto, Guid userId)
    {
        var item = await _itemRepo.GetByIdAsync(dto.PhysicalItemId);
        if (item == null) return ServiceResult<CirculationRecordDto>.Fail("Physical item not found");
        if (item.CirculationStatus != CirculationStatus.Available)
            return ServiceResult<CirculationRecordDto>.Fail($"Item is not available (current: {item.CirculationStatus})");
        if (item.IsOnLegalHold)
            return ServiceResult<CirculationRecordDto>.Fail("Cannot check out an item under legal hold");

        var record = new CirculationRecord
        {
            PhysicalItemId = dto.PhysicalItemId,
            BorrowerId = dto.BorrowerId,
            BorrowerStructureId = dto.BorrowerStructureId,
            Purpose = dto.Purpose,
            CheckedOutAt = DateTime.Now,
            CheckedOutBy = userId,
            DueDate = DateTime.Now.AddDays(dto.DueDays),
            ConditionAtCheckout = item.Condition,
            Status = CirculationRecordStatus.Active,
            CreatedBy = userId
        };

        var id = await _repo.CreateAsync(record);
        item.CirculationStatus = CirculationStatus.CheckedOut;
        await _itemRepo.UpdateAsync(item);

        // Record custody transfer
        await _custodyService.RecordTransferAsync(dto.PhysicalItemId, new CreateCustodyTransferDto
        {
            ToUserId = dto.BorrowerId,
            TransferType = "Circulation",
            ConditionAtTransfer = item.Condition.ToString()
        }, userId);

        record.Id = id;
        return ServiceResult<CirculationRecordDto>.Ok(MapToDto(record), "Item checked out");
    }

    public async Task<ServiceResult> ReturnAsync(Guid circulationId, ReturnPhysicalItemDto dto, Guid userId)
    {
        var record = await _repo.GetByIdAsync(circulationId);
        if (record == null) return ServiceResult.Fail("Circulation record not found");
        if (record.Status == CirculationRecordStatus.Returned)
            return ServiceResult.Fail("Item already returned");

        Enum.TryParse<ItemCondition>(dto.ConditionAtReturn, true, out var condition);
        record.ReturnedAt = DateTime.Now;
        record.ReturnedTo = userId;
        record.ConditionAtReturn = condition;
        record.ConditionNotes = dto.ConditionNotes;
        record.Status = CirculationRecordStatus.Returned;
        await _repo.UpdateAsync(record);

        var item = await _itemRepo.GetByIdAsync(record.PhysicalItemId);
        if (item != null)
        {
            item.CirculationStatus = CirculationStatus.Available;
            item.Condition = condition;
            await _itemRepo.UpdateAsync(item);
        }

        return ServiceResult.Ok("Item returned");
    }

    public async Task<ServiceResult> RenewAsync(Guid circulationId, Guid userId)
    {
        var record = await _repo.GetByIdAsync(circulationId);
        if (record == null) return ServiceResult.Fail("Circulation record not found");
        if (record.RenewalCount >= record.MaxRenewals)
            return ServiceResult.Fail($"Maximum renewals ({record.MaxRenewals}) reached");

        record.RenewalCount++;
        record.DueDate = record.DueDate.AddDays(14);
        await _repo.UpdateAsync(record);
        return ServiceResult.Ok($"Renewed. New due date: {record.DueDate:yyyy-MM-dd}");
    }

    public async Task<ServiceResult<List<CirculationRecordDto>>> GetActiveLoansAsync()
    {
        var records = await _repo.GetActiveLoansAsync();
        return ServiceResult<List<CirculationRecordDto>>.Ok(records.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<CirculationRecordDto>>> GetOverdueAsync()
    {
        var records = await _repo.GetOverdueAsync();
        return ServiceResult<List<CirculationRecordDto>>.Ok(records.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<CirculationRecordDto>>> GetItemHistoryAsync(Guid physicalItemId)
    {
        var records = await _repo.GetByItemIdAsync(physicalItemId);
        return ServiceResult<List<CirculationRecordDto>>.Ok(records.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult> ReportLostAsync(Guid circulationId, Guid userId)
    {
        var record = await _repo.GetByIdAsync(circulationId);
        if (record == null) return ServiceResult.Fail("Circulation record not found");
        record.Status = CirculationRecordStatus.Lost;
        await _repo.UpdateAsync(record);

        var item = await _itemRepo.GetByIdAsync(record.PhysicalItemId);
        if (item != null)
        {
            item.CirculationStatus = CirculationStatus.Lost;
            await _itemRepo.UpdateAsync(item);
        }

        await _activityLog.LogActivityAsync("ReportedLost", "PhysicalItem", record.PhysicalItemId,
            item?.Title ?? "", null, userId, null, null);
        return ServiceResult.Ok("Item reported as lost");
    }

    private static CirculationRecordDto MapToDto(CirculationRecord e) => new()
    {
        Id = e.Id, PhysicalItemId = e.PhysicalItemId, BorrowerId = e.BorrowerId,
        Purpose = e.Purpose, CheckedOutAt = e.CheckedOutAt, DueDate = e.DueDate,
        RenewalCount = e.RenewalCount, MaxRenewals = e.MaxRenewals,
        ReturnedAt = e.ReturnedAt, ConditionAtCheckout = e.ConditionAtCheckout.ToString(),
        ConditionAtReturn = e.ConditionAtReturn?.ToString(),
        Status = e.Status.ToString()
    };
}

public class CustodyService : ICustodyService
{
    private readonly ICustodyTransferRepository _repo;

    public CustodyService(ICustodyTransferRepository repo) => _repo = repo;

    public async Task<ServiceResult<CustodyTransferDto>> RecordTransferAsync(
        Guid physicalItemId, CreateCustodyTransferDto dto, Guid userId)
    {
        Enum.TryParse<CustodyTransferType>(dto.TransferType, true, out var transferType);
        Enum.TryParse<ItemCondition>(dto.ConditionAtTransfer, true, out var condition);

        // Get previous entry hash for chain
        var previousEntry = await _repo.GetLatestByItemIdAsync(physicalItemId);

        var transfer = new CustodyTransfer
        {
            PhysicalItemId = physicalItemId,
            FromUserId = userId,
            ToUserId = dto.ToUserId,
            ToLocationId = dto.ToLocationId,
            TransferType = transferType,
            ConditionAtTransfer = condition,
            TransferredAt = DateTime.Now,
            TransferredBy = userId,
            PreviousEntryHash = previousEntry?.EntryHash
        };

        // Compute hash for tamper detection
        var hashInput = $"{transfer.PhysicalItemId}|{transfer.FromUserId}|{transfer.ToUserId}|{transfer.TransferredAt:O}|{transfer.PreviousEntryHash}";
        transfer.EntryHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput))).ToLowerInvariant();

        var id = await _repo.CreateAsync(transfer);
        transfer.Id = id;

        return ServiceResult<CustodyTransferDto>.Ok(MapToDto(transfer), "Custody transfer recorded");
    }

    public async Task<ServiceResult> AcknowledgeTransferAsync(Guid transferId, Guid userId)
    {
        var transfer = await _repo.GetByIdAsync(transferId);
        if (transfer == null) return ServiceResult.Fail("Transfer not found");
        if (transfer.IsAcknowledged) return ServiceResult.Fail("Already acknowledged");

        transfer.IsAcknowledged = true;
        transfer.AcknowledgedAt = DateTime.Now;
        await _repo.UpdateAsync(transfer);
        return ServiceResult.Ok("Transfer acknowledged");
    }

    public async Task<ServiceResult<List<CustodyTransferDto>>> GetChainOfCustodyAsync(Guid physicalItemId)
    {
        var transfers = await _repo.GetByItemIdAsync(physicalItemId);
        return ServiceResult<List<CustodyTransferDto>>.Ok(transfers.Select(MapToDto).ToList());
    }

    private static CustodyTransferDto MapToDto(CustodyTransfer e) => new()
    {
        Id = e.Id, PhysicalItemId = e.PhysicalItemId, FromUserId = e.FromUserId,
        ToUserId = e.ToUserId, FromLocationId = e.FromLocationId, ToLocationId = e.ToLocationId,
        TransferType = e.TransferType.ToString(), ConditionAtTransfer = e.ConditionAtTransfer?.ToString(),
        IsAcknowledged = e.IsAcknowledged, AcknowledgedAt = e.AcknowledgedAt,
        TransferredAt = e.TransferredAt, EntryHash = e.EntryHash
    };
}

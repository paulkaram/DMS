using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepo;
    private readonly IDocumentRepository _documentRepo;

    public DashboardService(IDashboardRepository dashboardRepo, IDocumentRepository documentRepo)
    {
        _dashboardRepo = dashboardRepo;
        _documentRepo = documentRepo;
    }

    public async Task<ServiceResult<DashboardStatisticsDto>> GetStatisticsAsync(Guid userId)
    {
        var stats = new DashboardStatisticsDto
        {
            TotalCabinets = await _dashboardRepo.GetCabinetCountAsync(),
            TotalFolders = await _dashboardRepo.GetFolderCountAsync(),
            TotalDocuments = await _dashboardRepo.GetDocumentCountAsync(),
            TotalUsers = await _dashboardRepo.GetUserCountAsync(),
            DocumentsThisMonth = await _dashboardRepo.GetDocumentsThisMonthAsync(),
            DocumentsThisYear = await _dashboardRepo.GetDocumentsThisYearAsync(),
            TotalStorageUsed = await _dashboardRepo.GetTotalStorageUsedAsync(),
            MyCheckoutsCount = await _dashboardRepo.GetCheckedOutCountAsync(userId)
        };

        var contentTypes = await _dashboardRepo.GetContentTypeDistributionAsync();
        stats.ContentTypeDistribution = contentTypes.Select(x => new ContentTypeStatDto
        {
            ContentType = x.ContentType,
            Count = x.Count,
            TotalSize = x.TotalSize
        }).ToList();

        return ServiceResult<DashboardStatisticsDto>.Ok(stats);
    }

    public async Task<ServiceResult<List<RecentDocumentDto>>> GetRecentDocumentsAsync(int take = 10)
    {
        var docs = await _dashboardRepo.GetRecentDocumentsAsync(take);
        return ServiceResult<List<RecentDocumentDto>>.Ok(docs.Select(x => new RecentDocumentDto
        {
            Id = x.Id,
            Name = x.Name,
            FolderName = x.FolderName,
            Extension = x.Extension,
            CreatedAt = x.CreatedAt,
            CreatedByName = x.CreatedByName
        }).ToList());
    }

    public async Task<ServiceResult<List<DocumentDto>>> GetMyCheckedOutDocumentsAsync(Guid userId)
    {
        var docs = await _documentRepo.GetCheckedOutByUserAsync(userId);
        return ServiceResult<List<DocumentDto>>.Ok(docs.Select(d => new DocumentDto
        {
            Id = d.Id,
            FolderId = d.FolderId,
            Name = d.Name,
            Description = d.Description,
            Extension = d.Extension,
            ContentType = d.ContentType,
            Size = d.Size,
            CurrentVersion = d.CurrentVersion,
            IsCheckedOut = d.IsCheckedOut,
            CheckedOutBy = d.CheckedOutBy,
            CheckedOutAt = d.CheckedOutAt,
            CreatedAt = d.CreatedAt,
            ModifiedAt = d.ModifiedAt
        }).ToList());
    }
}

using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class AccessReviewService : IAccessReviewService
{
    private readonly DmsDbContext _context;
    private readonly ILogger<AccessReviewService> _logger;

    public AccessReviewService(DmsDbContext context, ILogger<AccessReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<AccessReviewCampaignDto>> CreateCampaignAsync(
        CreateAccessReviewCampaignDto dto, Guid userId)
    {
        var campaign = new AccessReviewCampaign
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Status = AccessReviewStatus.Open,
            StartDate = DateTime.Now,
            DueDate = dto.DueDate,
            ReviewerId = dto.ReviewerId,
            CreatedBy = userId
        };

        _context.AccessReviewCampaigns.Add(campaign);

        // Populate entries from current effective permissions
        var permissions = await _context.EffectivePermissions.ToListAsync();
        foreach (var perm in permissions)
        {
            _context.AccessReviewEntries.Add(new AccessReviewEntry
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                UserId = perm.UserId,
                NodeType = perm.NodeType.ToString(),
                NodeId = perm.NodeId,
                PermissionLevel = (int)perm.EffectiveLevel,
                PermissionSource = perm.SourceType
            });
        }

        campaign.TotalEntries = permissions.Count;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Access review campaign '{Name}' created with {Count} entries",
            dto.Name, permissions.Count);

        return ServiceResult<AccessReviewCampaignDto>.Ok(MapCampaignToDto(campaign));
    }

    public async Task<ServiceResult<AccessReviewCampaignDto>> GetCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.AccessReviewCampaigns.FindAsync(campaignId);
        if (campaign == null)
            return ServiceResult<AccessReviewCampaignDto>.Fail("Campaign not found");
        return ServiceResult<AccessReviewCampaignDto>.Ok(MapCampaignToDto(campaign));
    }

    public async Task<ServiceResult<List<AccessReviewCampaignDto>>> GetCampaignsAsync()
    {
        var campaigns = await _context.AccessReviewCampaigns
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
        return ServiceResult<List<AccessReviewCampaignDto>>.Ok(campaigns.Select(MapCampaignToDto).ToList());
    }

    public async Task<ServiceResult<List<CampaignReviewEntryDto>>> GetCampaignEntriesAsync(Guid campaignId)
    {
        var entries = await _context.AccessReviewEntries
            .Where(e => e.CampaignId == campaignId)
            .ToListAsync();

        return ServiceResult<List<CampaignReviewEntryDto>>.Ok(entries.Select(e => new CampaignReviewEntryDto
        {
            Id = e.Id,
            CampaignId = e.CampaignId,
            UserId = e.UserId,
            NodeType = e.NodeType,
            NodeId = e.NodeId,
            PermissionLevel = e.PermissionLevel,
            PermissionSource = e.PermissionSource,
            Decision = e.Decision.ToString(),
            Comments = e.Comments,
            DecidedAt = e.DecidedAt
        }).ToList());
    }

    public async Task<ServiceResult> SubmitReviewDecisionAsync(Guid entryId, SubmitAccessReviewDto dto, Guid userId)
    {
        var entry = await _context.AccessReviewEntries.FindAsync(entryId);
        if (entry == null)
            return ServiceResult.Fail("Entry not found");

        if (!Enum.TryParse<AccessReviewDecision>(dto.Decision, true, out var decision))
            return ServiceResult.Fail("Invalid decision");

        entry.Decision = decision;
        entry.Comments = dto.Comments;
        entry.DecidedBy = userId;
        entry.DecidedAt = DateTime.Now;

        // Update campaign progress
        var campaign = await _context.AccessReviewCampaigns.FindAsync(entry.CampaignId);
        if (campaign != null)
        {
            campaign.CompletedEntries = await _context.AccessReviewEntries
                .CountAsync(e => e.CampaignId == campaign.Id && e.Decision != AccessReviewDecision.Pending);

            if (campaign.CompletedEntries >= campaign.TotalEntries)
                campaign.Status = AccessReviewStatus.Completed;
            else
                campaign.Status = AccessReviewStatus.InProgress;
        }

        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Review decision submitted");
    }

    public async Task<ServiceResult<List<StalePermissionDto>>> GetStalePermissionsAsync(int inactiveDays)
    {
        var cutoffDate = DateTime.Now.AddDays(-inactiveDays);
        var staleUsers = await _context.Users
            .Where(u => u.IsActive && (u.LastLoginAt == null || u.LastLoginAt < cutoffDate))
            .ToListAsync();

        var staleUserIds = staleUsers.Select(u => u.Id).ToHashSet();

        var stalePermissions = await _context.EffectivePermissions
            .Where(p => staleUserIds.Contains(p.UserId))
            .ToListAsync();

        var result = stalePermissions.Select(p =>
        {
            var user = staleUsers.FirstOrDefault(u => u.Id == p.UserId);
            return new StalePermissionDto
            {
                UserId = p.UserId,
                UserName = user?.Username,
                NodeType = p.NodeType.ToString(),
                NodeId = p.NodeId,
                PermissionLevel = (int)p.EffectiveLevel,
                LastLoginAt = user?.LastLoginAt,
                InactiveDays = user?.LastLoginAt.HasValue == true
                    ? (int)(DateTime.Now - user.LastLoginAt.Value).TotalDays
                    : -1
            };
        }).ToList();

        return ServiceResult<List<StalePermissionDto>>.Ok(result);
    }

    private static AccessReviewCampaignDto MapCampaignToDto(AccessReviewCampaign c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description,
        Status = c.Status.ToString(), StartDate = c.StartDate, DueDate = c.DueDate,
        ReviewerId = c.ReviewerId, TotalEntries = c.TotalEntries,
        CompletedEntries = c.CompletedEntries, CreatedAt = c.CreatedAt
    };
}

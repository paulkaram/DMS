using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IAccessReviewService
{
    Task<ServiceResult<AccessReviewCampaignDto>> CreateCampaignAsync(CreateAccessReviewCampaignDto dto, Guid userId);
    Task<ServiceResult<AccessReviewCampaignDto>> GetCampaignAsync(Guid campaignId);
    Task<ServiceResult<List<AccessReviewCampaignDto>>> GetCampaignsAsync();
    Task<ServiceResult<List<CampaignReviewEntryDto>>> GetCampaignEntriesAsync(Guid campaignId);
    Task<ServiceResult> SubmitReviewDecisionAsync(Guid entryId, SubmitAccessReviewDto dto, Guid userId);
    Task<ServiceResult<List<StalePermissionDto>>> GetStalePermissionsAsync(int inactiveDays);
}

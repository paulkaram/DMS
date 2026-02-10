using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ApprovalWorkflowRepository : IApprovalWorkflowRepository
{
    private readonly DmsDbContext _context;

    public ApprovalWorkflowRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalWorkflow?> GetByIdAsync(Guid id)
    {
        var workflow = await _context.ApprovalWorkflows
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new ApprovalWorkflow
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                FolderId = w.FolderId,
                RequiredApprovers = w.RequiredApprovers,
                IsSequential = w.IsSequential,
                IsActive = w.IsActive,
                CreatedBy = w.CreatedBy,
                CreatedAt = w.CreatedAt,
                FolderName = _context.Folders
                    .Where(f => f.Id == w.FolderId)
                    .Select(f => f.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (workflow != null)
        {
            workflow.Steps = (await GetStepsAsync(id)).ToList();
        }

        return workflow;
    }

    public async Task<IEnumerable<ApprovalWorkflow>> GetAllAsync()
    {
        return await _context.ApprovalWorkflows
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new ApprovalWorkflow
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                FolderId = w.FolderId,
                RequiredApprovers = w.RequiredApprovers,
                IsSequential = w.IsSequential,
                IsActive = w.IsActive,
                CreatedBy = w.CreatedBy,
                CreatedAt = w.CreatedAt,
                FolderName = _context.Folders
                    .Where(f => f.Id == w.FolderId)
                    .Select(f => f.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<ApprovalWorkflow?> GetByFolderIdAsync(Guid folderId)
    {
        return await _context.ApprovalWorkflows
            .AsNoTracking()
            .Where(w => w.FolderId == folderId)
            .Select(w => new ApprovalWorkflow
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                FolderId = w.FolderId,
                RequiredApprovers = w.RequiredApprovers,
                IsSequential = w.IsSequential,
                IsActive = w.IsActive,
                CreatedBy = w.CreatedBy,
                CreatedAt = w.CreatedAt,
                FolderName = _context.Folders
                    .Where(f => f.Id == w.FolderId)
                    .Select(f => f.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ApprovalWorkflowStep>> GetStepsAsync(Guid workflowId)
    {
        return await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .Where(s => s.WorkflowId == workflowId)
            .OrderBy(s => s.StepOrder)
            .Select(s => new ApprovalWorkflowStep
            {
                Id = s.Id,
                WorkflowId = s.WorkflowId,
                StepOrder = s.StepOrder,
                ApproverUserId = s.ApproverUserId,
                ApproverRoleId = s.ApproverRoleId,
                IsRequired = s.IsRequired,
                CreatedAt = s.CreatedAt,
                ApproverUserName = _context.Users
                    .Where(u => u.Id == s.ApproverUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ApproverRoleName = _context.Roles
                    .Where(r => r.Id == s.ApproverRoleId)
                    .Select(r => r.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ApprovalWorkflow entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.ApprovalWorkflows.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ApprovalWorkflow entity)
    {
        var existing = await _context.ApprovalWorkflows.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.Name = entity.Name;
        existing.Description = entity.Description;
        existing.FolderId = entity.FolderId;
        existing.RequiredApprovers = entity.RequiredApprovers;
        existing.IsSequential = entity.IsSequential;
        existing.IsActive = entity.IsActive;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.ApprovalWorkflows.FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = false;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Guid> AddStepAsync(ApprovalWorkflowStep step)
    {
        step.Id = Guid.NewGuid();
        step.CreatedAt = DateTime.UtcNow;

        _context.ApprovalWorkflowSteps.Add(step);
        await _context.SaveChangesAsync();

        return step.Id;
    }

    public async Task<bool> RemoveStepAsync(Guid stepId)
    {
        var entity = await _context.ApprovalWorkflowSteps.FindAsync(stepId);
        if (entity == null) return false;

        _context.ApprovalWorkflowSteps.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }
}

public class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly DmsDbContext _context;

    public ApprovalRequestRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalRequest?> GetByIdAsync(Guid id)
    {
        var request = await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ar.Id == id)
            .Select(ar => new ApprovalRequest
            {
                Id = ar.Id,
                DocumentId = ar.DocumentId,
                WorkflowId = ar.WorkflowId,
                RequestedBy = ar.RequestedBy,
                Status = ar.Status,
                DueDate = ar.DueDate,
                Comments = ar.Comments,
                CreatedAt = ar.CreatedAt,
                CompletedAt = ar.CompletedAt,
                DocumentName = _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.Id == ar.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                RequestedByName = _context.Users
                    .Where(u => u.Id == ar.RequestedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                WorkflowName = _context.ApprovalWorkflows
                    .IgnoreQueryFilters()
                    .Where(w => w.Id == ar.WorkflowId)
                    .Select(w => w.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (request != null)
        {
            request.Actions = (await GetActionsAsync(id)).ToList();
        }

        return request;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ar.DocumentId == documentId)
            .OrderByDescending(ar => ar.CreatedAt)
            .Select(ar => new ApprovalRequest
            {
                Id = ar.Id,
                DocumentId = ar.DocumentId,
                WorkflowId = ar.WorkflowId,
                RequestedBy = ar.RequestedBy,
                Status = ar.Status,
                DueDate = ar.DueDate,
                Comments = ar.Comments,
                CreatedAt = ar.CreatedAt,
                CompletedAt = ar.CompletedAt,
                DocumentName = _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.Id == ar.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                RequestedByName = _context.Users
                    .Where(u => u.Id == ar.RequestedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                WorkflowName = _context.ApprovalWorkflows
                    .IgnoreQueryFilters()
                    .Where(w => w.Id == ar.WorkflowId)
                    .Select(w => w.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalRequest>> GetPendingForUserAsync(Guid userId)
    {
        // Complex query with multiple JOINs and NOT EXISTS - using raw SQL
        return await _context.Database.SqlQueryRaw<ApprovalRequest>(@"
            SELECT DISTINCT ar.Id, ar.DocumentId, ar.WorkflowId, ar.RequestedBy,
                   ar.Status, ar.DueDate, ar.Comments, ar.CreatedAt, ar.CompletedAt,
                   d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            LEFT JOIN ApprovalWorkflowSteps aws ON aw.Id = aws.WorkflowId
            LEFT JOIN UserRoles ur ON aws.ApproverRoleId = ur.RoleId
            WHERE ar.Status = 0
            AND (aws.ApproverUserId = {0} OR ur.UserId = {0})
            AND NOT EXISTS (
                SELECT 1 FROM ApprovalActions aa
                WHERE aa.RequestId = ar.Id AND aa.ApproverId = {0}
            )
            ORDER BY ar.CreatedAt DESC",
            userId).ToListAsync();
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByRequestedByAsync(Guid userId)
    {
        return await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ar.RequestedBy == userId)
            .OrderByDescending(ar => ar.CreatedAt)
            .Select(ar => new ApprovalRequest
            {
                Id = ar.Id,
                DocumentId = ar.DocumentId,
                WorkflowId = ar.WorkflowId,
                RequestedBy = ar.RequestedBy,
                Status = ar.Status,
                DueDate = ar.DueDate,
                Comments = ar.Comments,
                CreatedAt = ar.CreatedAt,
                CompletedAt = ar.CompletedAt,
                DocumentName = _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.Id == ar.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                RequestedByName = _context.Users
                    .Where(u => u.Id == ar.RequestedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                WorkflowName = _context.ApprovalWorkflows
                    .IgnoreQueryFilters()
                    .Where(w => w.Id == ar.WorkflowId)
                    .Select(w => w.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalRequest>> GetAllAsync(int? status = null)
    {
        var query = _context.ApprovalRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(ar => ar.Status == status.Value);

        return await query
            .OrderByDescending(ar => ar.CreatedAt)
            .Select(ar => new ApprovalRequest
            {
                Id = ar.Id,
                DocumentId = ar.DocumentId,
                WorkflowId = ar.WorkflowId,
                RequestedBy = ar.RequestedBy,
                Status = ar.Status,
                DueDate = ar.DueDate,
                Comments = ar.Comments,
                CreatedAt = ar.CreatedAt,
                CompletedAt = ar.CompletedAt,
                DocumentName = _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.Id == ar.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                RequestedByName = _context.Users
                    .Where(u => u.Id == ar.RequestedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                WorkflowName = _context.ApprovalWorkflows
                    .IgnoreQueryFilters()
                    .Where(w => w.Id == ar.WorkflowId)
                    .Select(w => w.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ApprovalAction>> GetActionsAsync(Guid requestId)
    {
        return await _context.ApprovalActions
            .AsNoTracking()
            .Where(aa => aa.RequestId == requestId)
            .OrderBy(aa => aa.ActionDate)
            .Select(aa => new ApprovalAction
            {
                Id = aa.Id,
                RequestId = aa.RequestId,
                StepId = aa.StepId,
                ApproverId = aa.ApproverId,
                Action = aa.Action,
                Comments = aa.Comments,
                ActionDate = aa.ActionDate,
                ApproverName = _context.Users
                    .Where(u => u.Id == aa.ApproverId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ApprovalRequest entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.ApprovalRequests.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, int status, DateTime? completedAt = null)
    {
        var entity = await _context.ApprovalRequests.FindAsync(id);
        if (entity == null) return false;

        entity.Status = status;
        entity.CompletedAt = completedAt ?? DateTime.UtcNow;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Guid> AddActionAsync(ApprovalAction action)
    {
        action.Id = Guid.NewGuid();
        action.ActionDate = DateTime.UtcNow;

        _context.ApprovalActions.Add(action);
        await _context.SaveChangesAsync();

        return action.Id;
    }
}

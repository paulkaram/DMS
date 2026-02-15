using DMS.DAL.DTOs;
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
                DesignerData = w.DesignerData,
                TriggerType = w.TriggerType,
                InheritToSubfolders = w.InheritToSubfolders,
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
                DesignerData = w.DesignerData,
                TriggerType = w.TriggerType,
                InheritToSubfolders = w.InheritToSubfolders,
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
                DesignerData = w.DesignerData,
                TriggerType = w.TriggerType,
                InheritToSubfolders = w.InheritToSubfolders,
                FolderName = _context.Folders
                    .Where(f => f.Id == w.FolderId)
                    .Select(f => f.Name)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ApprovalWorkflow?> GetActiveByFolderChainAsync(Guid folderId, string triggerType)
    {
        // Walk up the folder parent chain via recursive CTE and find the first
        // active workflow matching the trigger type that either:
        // - Is directly assigned to the target folder, OR
        // - Is assigned to a parent folder with InheritToSubfolders = true
        // Note: SqlQueryRaw<ApprovalWorkflow> can't handle the Steps navigation property,
        // so we query just the Id and then load via GetByIdAsync.
        var ids = await _context.Database.SqlQueryRaw<Guid>(@"
            WITH FolderChain AS (
                SELECT Id, ParentFolderId, 0 AS Depth
                FROM Folders
                WHERE Id = {0} AND IsActive = 1
                UNION ALL
                SELECT f.Id, f.ParentFolderId, fc.Depth + 1
                FROM Folders f
                INNER JOIN FolderChain fc ON f.Id = fc.ParentFolderId
                WHERE f.IsActive = 1
            )
            SELECT TOP 1 aw.Id
            FROM FolderChain fc
            INNER JOIN ApprovalWorkflows aw ON aw.FolderId = fc.Id
            WHERE aw.IsActive = 1
              AND aw.TriggerType = {1}
              AND (fc.Depth = 0 OR aw.InheritToSubfolders = 1)
            ORDER BY fc.Depth ASC",
            folderId, triggerType).ToListAsync();

        if (ids.Count == 0) return null;
        return await GetByIdAsync(ids[0]);
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
                ApproverStructureId = s.ApproverStructureId,
                AssignToManager = s.AssignToManager,
                IsRequired = s.IsRequired,
                StatusId = s.StatusId,
                CreatedAt = s.CreatedAt,
                ApproverUserName = _context.Users
                    .Where(u => u.Id == s.ApproverUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ApproverRoleName = _context.Roles
                    .Where(r => r.Id == s.ApproverRoleId)
                    .Select(r => r.Name)
                    .FirstOrDefault(),
                ApproverStructureName = _context.Structures
                    .Where(st => st.Id == s.ApproverStructureId)
                    .Select(st => st.Name)
                    .FirstOrDefault(),
                StatusName = _context.WorkflowStatuses
                    .Where(ws => ws.Id == s.StatusId)
                    .Select(ws => ws.Name)
                    .FirstOrDefault(),
                StatusColor = _context.WorkflowStatuses
                    .Where(ws => ws.Id == s.StatusId)
                    .Select(ws => ws.Color)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ApprovalWorkflow entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

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
        existing.DesignerData = entity.DesignerData;
        existing.TriggerType = entity.TriggerType;
        existing.InheritToSubfolders = entity.InheritToSubfolders;

        await _context.SaveChangesAsync();
        return true;
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
        step.CreatedAt = DateTime.Now;

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

    public async Task ReplaceStepsAsync(Guid workflowId, IEnumerable<ApprovalWorkflowStep> newSteps)
    {
        var existing = await _context.ApprovalWorkflowSteps
            .Where(s => s.WorkflowId == workflowId)
            .ToListAsync();
        _context.ApprovalWorkflowSteps.RemoveRange(existing);

        foreach (var step in newSteps)
        {
            step.Id = Guid.NewGuid();
            step.WorkflowId = workflowId;
            step.CreatedAt = DateTime.Now;
            _context.ApprovalWorkflowSteps.Add(step);
        }

        await _context.SaveChangesAsync();
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
            .GroupJoin(_context.Documents.IgnoreQueryFilters().AsNoTracking(), ar => ar.DocumentId, d => d.Id, (ar, docs) => new { ar, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ar, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.ar.RequestedBy, u => u.Id, (x, users) => new { x.ar, x.d, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.ar, x.d, u })
            .GroupJoin(_context.ApprovalWorkflows.IgnoreQueryFilters().AsNoTracking(), x => x.ar.WorkflowId, w => w.Id, (x, workflows) => new { x.ar, x.d, x.u, workflows })
            .SelectMany(x => x.workflows.DefaultIfEmpty(), (x, w) => new { x.ar, x.d, x.u, w })
            .Select(x => new ApprovalRequest
            {
                Id = x.ar.Id,
                DocumentId = x.ar.DocumentId,
                WorkflowId = x.ar.WorkflowId,
                RequestedBy = x.ar.RequestedBy,
                Status = x.ar.Status,
                DueDate = x.ar.DueDate,
                Comments = x.ar.Comments,
                CreatedAt = x.ar.CreatedAt,
                CompletedAt = x.ar.CompletedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                RequestedByName = x.u != null ? x.u.DisplayName : null,
                WorkflowName = x.w != null ? x.w.Name : null
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
        var requests = await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ar.DocumentId == documentId)
            .GroupJoin(_context.Documents.IgnoreQueryFilters().AsNoTracking(), ar => ar.DocumentId, d => d.Id, (ar, docs) => new { ar, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ar, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.ar.RequestedBy, u => u.Id, (x, users) => new { x.ar, x.d, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.ar, x.d, u })
            .GroupJoin(_context.ApprovalWorkflows.IgnoreQueryFilters().AsNoTracking(), x => x.ar.WorkflowId, w => w.Id, (x, workflows) => new { x.ar, x.d, x.u, workflows })
            .SelectMany(x => x.workflows.DefaultIfEmpty(), (x, w) => new { x.ar, x.d, x.u, w })
            .OrderByDescending(x => x.ar.CreatedAt)
            .Select(x => new ApprovalRequest
            {
                Id = x.ar.Id,
                DocumentId = x.ar.DocumentId,
                WorkflowId = x.ar.WorkflowId,
                RequestedBy = x.ar.RequestedBy,
                Status = x.ar.Status,
                DueDate = x.ar.DueDate,
                Comments = x.ar.Comments,
                CreatedAt = x.ar.CreatedAt,
                CompletedAt = x.ar.CompletedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                RequestedByName = x.u != null ? x.u.DisplayName : null,
                WorkflowName = x.w != null ? x.w.Name : null
            })
            .ToListAsync();

        // Batch-load actions for all requests
        if (requests.Count > 0)
        {
            var requestIds = requests.Select(r => r.Id).ToList();
            var allActions = await _context.ApprovalActions
                .AsNoTracking()
                .Where(aa => requestIds.Contains(aa.RequestId))
                .GroupJoin(_context.Users.AsNoTracking(), aa => aa.ApproverId, u => u.Id, (aa, users) => new { aa, users })
                .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.aa, u })
                .OrderBy(x => x.aa.ActionDate)
                .Select(x => new ApprovalAction
                {
                    Id = x.aa.Id,
                    RequestId = x.aa.RequestId,
                    StepId = x.aa.StepId,
                    ApproverId = x.aa.ApproverId,
                    Action = x.aa.Action,
                    Comments = x.aa.Comments,
                    ActionDate = x.aa.ActionDate,
                    ApproverName = x.u != null ? x.u.DisplayName : null
                })
                .ToListAsync();

            var actionsByRequest = allActions.GroupBy(a => a.RequestId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var r in requests)
            {
                r.Actions = actionsByRequest.GetValueOrDefault(r.Id) ?? [];
            }
        }

        return requests;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetPendingForUserAsync(Guid userId)
    {
        // Complex query with multiple JOINs and NOT EXISTS - using FromSqlRaw
        // (SqlQueryRaw doesn't work with mapped entity types)
        var requests = await _context.ApprovalRequests.FromSqlRaw(@"
            SELECT DISTINCT ar.Id, ar.DocumentId, ar.WorkflowId, ar.RequestedBy,
                   ar.Status, ar.DueDate, ar.Comments, ar.CreatedAt, ar.CompletedAt
            FROM ApprovalRequests ar
            INNER JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            INNER JOIN ApprovalWorkflowSteps aws ON aw.Id = aws.WorkflowId
            LEFT JOIN UserRoles ur ON aws.ApproverRoleId = ur.RoleId
                AND ur.UserId = {0}
            LEFT JOIN StructureMembers sm ON aws.ApproverStructureId = sm.StructureId
                AND sm.UserId = {0}
            LEFT JOIN Structures st ON aws.ApproverStructureId = st.Id
            WHERE ar.Status = {1}
            AND (
                aws.ApproverUserId = {0}
                OR ur.UserId IS NOT NULL
                OR (aws.ApproverStructureId IS NOT NULL AND (
                    (aws.AssignToManager = 1 AND st.ManagerId = {0})
                    OR (aws.AssignToManager = 0 AND sm.UserId IS NOT NULL)
                ))
            )
            AND NOT EXISTS (
                SELECT 1 FROM ApprovalActions aa
                WHERE aa.RequestId = ar.Id AND aa.ApproverId = {0}
            )
            ORDER BY ar.CreatedAt DESC",
            userId, (int)ApprovalStatus.Pending).AsNoTracking().ToListAsync();

        // Enrich with display names
        if (requests.Count > 0)
        {
            var docIds = requests.Select(r => r.DocumentId).Distinct().ToList();
            var userIds = requests.Select(r => r.RequestedBy).Distinct().ToList();
            var workflowIds = requests.Where(r => r.WorkflowId.HasValue).Select(r => r.WorkflowId!.Value).Distinct().ToList();

            var docNames = await _context.Documents.IgnoreQueryFilters()
                .Where(d => docIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            var userNames = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

            var workflowNames = await _context.ApprovalWorkflows.IgnoreQueryFilters()
                .Where(w => workflowIds.Contains(w.Id))
                .Select(w => new { w.Id, w.Name })
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            foreach (var r in requests)
            {
                r.DocumentName = docNames.GetValueOrDefault(r.DocumentId);
                r.RequestedByName = userNames.GetValueOrDefault(r.RequestedBy);
                if (r.WorkflowId.HasValue)
                    r.WorkflowName = workflowNames.GetValueOrDefault(r.WorkflowId.Value);
            }
        }

        return requests;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByRequestedByAsync(Guid userId)
    {
        var requests = await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ar.RequestedBy == userId)
            .GroupJoin(_context.Documents.IgnoreQueryFilters().AsNoTracking(), ar => ar.DocumentId, d => d.Id, (ar, docs) => new { ar, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ar, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.ar.RequestedBy, u => u.Id, (x, users) => new { x.ar, x.d, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.ar, x.d, u })
            .GroupJoin(_context.ApprovalWorkflows.IgnoreQueryFilters().AsNoTracking(), x => x.ar.WorkflowId, w => w.Id, (x, workflows) => new { x.ar, x.d, x.u, workflows })
            .SelectMany(x => x.workflows.DefaultIfEmpty(), (x, w) => new { x.ar, x.d, x.u, w })
            .OrderByDescending(x => x.ar.CreatedAt)
            .Select(x => new ApprovalRequest
            {
                Id = x.ar.Id,
                DocumentId = x.ar.DocumentId,
                WorkflowId = x.ar.WorkflowId,
                RequestedBy = x.ar.RequestedBy,
                Status = x.ar.Status,
                DueDate = x.ar.DueDate,
                Comments = x.ar.Comments,
                CreatedAt = x.ar.CreatedAt,
                CompletedAt = x.ar.CompletedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                RequestedByName = x.u != null ? x.u.DisplayName : null,
                WorkflowName = x.w != null ? x.w.Name : null
            })
            .ToListAsync();

        // Batch-load actions for all requests
        if (requests.Count > 0)
        {
            var requestIds = requests.Select(r => r.Id).ToList();
            var allActions = await _context.ApprovalActions
                .AsNoTracking()
                .Where(aa => requestIds.Contains(aa.RequestId))
                .GroupJoin(_context.Users.AsNoTracking(), aa => aa.ApproverId, u => u.Id, (aa, users) => new { aa, users })
                .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.aa, u })
                .OrderBy(x => x.aa.ActionDate)
                .Select(x => new ApprovalAction
                {
                    Id = x.aa.Id,
                    RequestId = x.aa.RequestId,
                    StepId = x.aa.StepId,
                    ApproverId = x.aa.ApproverId,
                    Action = x.aa.Action,
                    Comments = x.aa.Comments,
                    ActionDate = x.aa.ActionDate,
                    ApproverName = x.u != null ? x.u.DisplayName : null
                })
                .ToListAsync();

            var actionsByRequest = allActions.GroupBy(a => a.RequestId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var r in requests)
            {
                r.Actions = actionsByRequest.GetValueOrDefault(r.Id) ?? [];
            }
        }

        return requests;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetAllAsync(int? status = null)
    {
        var query = _context.ApprovalRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(ar => ar.Status == status.Value);

        return await query
            .GroupJoin(_context.Documents.IgnoreQueryFilters().AsNoTracking(), ar => ar.DocumentId, d => d.Id, (ar, docs) => new { ar, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ar, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.ar.RequestedBy, u => u.Id, (x, users) => new { x.ar, x.d, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.ar, x.d, u })
            .GroupJoin(_context.ApprovalWorkflows.IgnoreQueryFilters().AsNoTracking(), x => x.ar.WorkflowId, w => w.Id, (x, workflows) => new { x.ar, x.d, x.u, workflows })
            .SelectMany(x => x.workflows.DefaultIfEmpty(), (x, w) => new { x.ar, x.d, x.u, w })
            .OrderByDescending(x => x.ar.CreatedAt)
            .Select(x => new ApprovalRequest
            {
                Id = x.ar.Id,
                DocumentId = x.ar.DocumentId,
                WorkflowId = x.ar.WorkflowId,
                RequestedBy = x.ar.RequestedBy,
                Status = x.ar.Status,
                DueDate = x.ar.DueDate,
                Comments = x.ar.Comments,
                CreatedAt = x.ar.CreatedAt,
                CompletedAt = x.ar.CompletedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                RequestedByName = x.u != null ? x.u.DisplayName : null,
                WorkflowName = x.w != null ? x.w.Name : null
            })
            .ToListAsync();
    }

    public async Task<PagedResult<ApprovalRequest>> GetAllPagedAsync(int page = 1, int pageSize = 50, int? status = null)
    {
        pageSize = Math.Min(pageSize, 200);
        var query = _context.ApprovalRequests.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(ar => ar.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .GroupJoin(_context.Documents.IgnoreQueryFilters().AsNoTracking(), ar => ar.DocumentId, d => d.Id, (ar, docs) => new { ar, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ar, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.ar.RequestedBy, u => u.Id, (x, users) => new { x.ar, x.d, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.ar, x.d, u })
            .GroupJoin(_context.ApprovalWorkflows.IgnoreQueryFilters().AsNoTracking(), x => x.ar.WorkflowId, w => w.Id, (x, workflows) => new { x.ar, x.d, x.u, workflows })
            .SelectMany(x => x.workflows.DefaultIfEmpty(), (x, w) => new { x.ar, x.d, x.u, w })
            .OrderByDescending(x => x.ar.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApprovalRequest
            {
                Id = x.ar.Id,
                DocumentId = x.ar.DocumentId,
                WorkflowId = x.ar.WorkflowId,
                RequestedBy = x.ar.RequestedBy,
                Status = x.ar.Status,
                DueDate = x.ar.DueDate,
                Comments = x.ar.Comments,
                CreatedAt = x.ar.CreatedAt,
                CompletedAt = x.ar.CompletedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                RequestedByName = x.u != null ? x.u.DisplayName : null,
                WorkflowName = x.w != null ? x.w.Name : null
            })
            .ToListAsync();
        return new PagedResult<ApprovalRequest> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<ApprovalAction>> GetActionsAsync(Guid requestId)
    {
        return await _context.ApprovalActions
            .AsNoTracking()
            .Where(aa => aa.RequestId == requestId)
            .GroupJoin(_context.Users.AsNoTracking(), aa => aa.ApproverId, u => u.Id, (aa, users) => new { aa, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.aa, u })
            .OrderBy(x => x.aa.ActionDate)
            .Select(x => new ApprovalAction
            {
                Id = x.aa.Id,
                RequestId = x.aa.RequestId,
                StepId = x.aa.StepId,
                ApproverId = x.aa.ApproverId,
                Action = x.aa.Action,
                Comments = x.aa.Comments,
                ActionDate = x.aa.ActionDate,
                ApproverName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ApprovalRequest entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.ApprovalRequests.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, int status, DateTime? completedAt = null)
    {
        var entity = await _context.ApprovalRequests.FindAsync(id);
        if (entity == null) return false;

        entity.Status = status;
        entity.CompletedAt = completedAt ?? DateTime.Now;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Guid> AddActionAsync(ApprovalAction action)
    {
        action.Id = Guid.NewGuid();
        action.ActionDate = DateTime.Now;

        _context.ApprovalActions.Add(action);
        await _context.SaveChangesAsync();

        return action.Id;
    }

    public async Task<Dictionary<Guid, int>> GetLatestStatusByDocumentIdsAsync(IEnumerable<Guid> documentIds)
    {
        var ids = documentIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, int>();

        return await _context.ApprovalRequests
            .AsNoTracking()
            .Where(ar => ids.Contains(ar.DocumentId))
            .GroupBy(ar => ar.DocumentId)
            .Select(g => new
            {
                DocumentId = g.Key,
                Status = g.OrderByDescending(ar => ar.CreatedAt).First().Status
            })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Status);
    }

    public async Task<bool> IsApproverForDocumentAsync(Guid documentId, Guid userId)
    {
        // Check if the user is an assigned approver for any pending request on this document
        var result = await _context.Database.SqlQueryRaw<int>(@"
            SELECT COUNT(*) AS [Value]
            FROM ApprovalRequests ar
            INNER JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            INNER JOIN ApprovalWorkflowSteps aws ON aw.Id = aws.WorkflowId
            LEFT JOIN UserRoles ur ON aws.ApproverRoleId = ur.RoleId
                AND ur.UserId = {1}
            LEFT JOIN StructureMembers sm ON aws.ApproverStructureId = sm.StructureId
                AND sm.UserId = {1}
            LEFT JOIN Structures st ON aws.ApproverStructureId = st.Id
            WHERE ar.DocumentId = {0}
            AND ar.Status = {2}
            AND (
                aws.ApproverUserId = {1}
                OR ur.UserId IS NOT NULL
                OR (aws.ApproverStructureId IS NOT NULL AND (
                    (aws.AssignToManager = 1 AND st.ManagerId = {1})
                    OR (aws.AssignToManager = 0 AND sm.UserId IS NOT NULL)
                ))
            )",
            documentId, userId, (int)ApprovalStatus.Pending).FirstOrDefaultAsync();

        return result > 0;
    }
}

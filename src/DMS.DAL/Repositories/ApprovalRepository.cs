using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class ApprovalWorkflowRepository : IApprovalWorkflowRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ApprovalWorkflowRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ApprovalWorkflow?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var workflow = await connection.QueryFirstOrDefaultAsync<ApprovalWorkflow>(@"
            SELECT aw.*, f.Name as FolderName
            FROM ApprovalWorkflows aw
            LEFT JOIN Folders f ON aw.FolderId = f.Id
            WHERE aw.Id = @Id",
            new { Id = id });

        if (workflow != null)
        {
            workflow.Steps = (await GetStepsAsync(id)).ToList();
        }

        return workflow;
    }

    public async Task<IEnumerable<ApprovalWorkflow>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalWorkflow>(@"
            SELECT aw.*, f.Name as FolderName
            FROM ApprovalWorkflows aw
            LEFT JOIN Folders f ON aw.FolderId = f.Id
            WHERE aw.IsActive = 1
            ORDER BY aw.Name");
    }

    public async Task<ApprovalWorkflow?> GetByFolderIdAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ApprovalWorkflow>(@"
            SELECT aw.*, f.Name as FolderName
            FROM ApprovalWorkflows aw
            LEFT JOIN Folders f ON aw.FolderId = f.Id
            WHERE aw.FolderId = @FolderId AND aw.IsActive = 1",
            new { FolderId = folderId });
    }

    public async Task<IEnumerable<ApprovalWorkflowStep>> GetStepsAsync(Guid workflowId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalWorkflowStep>(@"
            SELECT aws.*, u.DisplayName as ApproverUserName, r.Name as ApproverRoleName
            FROM ApprovalWorkflowSteps aws
            LEFT JOIN Users u ON aws.ApproverUserId = u.Id
            LEFT JOIN Roles r ON aws.ApproverRoleId = r.Id
            WHERE aws.WorkflowId = @WorkflowId
            ORDER BY aws.StepOrder",
            new { WorkflowId = workflowId });
    }

    public async Task<Guid> CreateAsync(ApprovalWorkflow entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ApprovalWorkflows (Id, Name, Description, FolderId,
                RequiredApprovers, IsSequential, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @FolderId,
                @RequiredApprovers, @IsSequential, @IsActive, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ApprovalWorkflow entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE ApprovalWorkflows
            SET Name = @Name, Description = @Description, FolderId = @FolderId,
                RequiredApprovers = @RequiredApprovers, IsSequential = @IsSequential,
                IsActive = @IsActive
            WHERE Id = @Id",
            entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE ApprovalWorkflows SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<Guid> AddStepAsync(ApprovalWorkflowStep step)
    {
        step.Id = Guid.NewGuid();
        step.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ApprovalWorkflowSteps (Id, WorkflowId, StepOrder,
                ApproverUserId, ApproverRoleId, IsRequired, CreatedAt)
            VALUES (@Id, @WorkflowId, @StepOrder,
                @ApproverUserId, @ApproverRoleId, @IsRequired, @CreatedAt)",
            step);

        return step.Id;
    }

    public async Task<bool> RemoveStepAsync(Guid stepId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM ApprovalWorkflowSteps WHERE Id = @Id", new { Id = stepId });
        return affected > 0;
    }
}

public class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ApprovalRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ApprovalRequest?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var request = await connection.QueryFirstOrDefaultAsync<ApprovalRequest>(@"
            SELECT ar.*, d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            WHERE ar.Id = @Id",
            new { Id = id });

        if (request != null)
        {
            request.Actions = (await GetActionsAsync(id)).ToList();
        }

        return request;
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalRequest>(@"
            SELECT ar.*, d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            WHERE ar.DocumentId = @DocumentId
            ORDER BY ar.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<ApprovalRequest>> GetPendingForUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalRequest>(@"
            SELECT DISTINCT ar.*, d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            LEFT JOIN ApprovalWorkflowSteps aws ON aw.Id = aws.WorkflowId
            LEFT JOIN UserRoles ur ON aws.ApproverRoleId = ur.RoleId
            WHERE ar.Status = 0
            AND (aws.ApproverUserId = @UserId OR ur.UserId = @UserId)
            AND NOT EXISTS (
                SELECT 1 FROM ApprovalActions aa
                WHERE aa.RequestId = ar.Id AND aa.ApproverId = @UserId
            )
            ORDER BY ar.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<ApprovalRequest>> GetByRequestedByAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalRequest>(@"
            SELECT ar.*, d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            WHERE ar.RequestedBy = @UserId
            ORDER BY ar.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<ApprovalRequest>> GetAllAsync(int? status = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT ar.*, d.Name as DocumentName, u.DisplayName as RequestedByName,
                   aw.Name as WorkflowName
            FROM ApprovalRequests ar
            LEFT JOIN Documents d ON ar.DocumentId = d.Id
            LEFT JOIN Users u ON ar.RequestedBy = u.Id
            LEFT JOIN ApprovalWorkflows aw ON ar.WorkflowId = aw.Id
            WHERE 1=1";

        if (status.HasValue)
            sql += " AND ar.Status = @Status";

        sql += " ORDER BY ar.CreatedAt DESC";

        return await connection.QueryAsync<ApprovalRequest>(sql, new { Status = status });
    }

    public async Task<IEnumerable<ApprovalAction>> GetActionsAsync(Guid requestId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApprovalAction>(@"
            SELECT aa.*, u.DisplayName as ApproverName
            FROM ApprovalActions aa
            LEFT JOIN Users u ON aa.ApproverId = u.Id
            WHERE aa.RequestId = @RequestId
            ORDER BY aa.ActionDate",
            new { RequestId = requestId });
    }

    public async Task<Guid> CreateAsync(ApprovalRequest entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ApprovalRequests (Id, DocumentId, WorkflowId, RequestedBy,
                Status, DueDate, Comments, CreatedAt)
            VALUES (@Id, @DocumentId, @WorkflowId, @RequestedBy,
                @Status, @DueDate, @Comments, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, int status, DateTime? completedAt = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE ApprovalRequests
            SET Status = @Status, CompletedAt = @CompletedAt
            WHERE Id = @Id",
            new { Id = id, Status = status, CompletedAt = completedAt ?? DateTime.UtcNow });
        return affected > 0;
    }

    public async Task<Guid> AddActionAsync(ApprovalAction action)
    {
        action.Id = Guid.NewGuid();
        action.ActionDate = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ApprovalActions (Id, RequestId, StepId, ApproverId, Action, Comments, ActionDate)
            VALUES (@Id, @RequestId, @StepId, @ApproverId, @Action, @Comments, @ActionDate)",
            action);

        return action.Id;
    }
}

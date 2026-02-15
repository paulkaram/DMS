-- Migration 018: Workflow Statuses
-- Adds WorkflowStatuses table and StatusId FK to ApprovalWorkflowSteps

-- Create WorkflowStatuses table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkflowStatuses')
BEGIN
    CREATE TABLE WorkflowStatuses (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(256) NOT NULL,
        Color NVARCHAR(20) NOT NULL DEFAULT '#6366f1',
        Icon NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Add StatusId column to ApprovalWorkflowSteps
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApprovalWorkflowSteps') AND name = 'StatusId')
BEGIN
    ALTER TABLE ApprovalWorkflowSteps
    ADD StatusId UNIQUEIDENTIFIER NULL;

    ALTER TABLE ApprovalWorkflowSteps
    ADD CONSTRAINT FK_ApprovalWorkflowSteps_WorkflowStatuses
    FOREIGN KEY (StatusId) REFERENCES WorkflowStatuses(Id);
END
GO

-- Seed default statuses
IF NOT EXISTS (SELECT 1 FROM WorkflowStatuses)
BEGIN
    INSERT INTO WorkflowStatuses (Id, Name, Color, Icon, Description, SortOrder, IsActive, CreatedAt) VALUES
    (NEWID(), 'Pending Review',   '#6366f1', 'hourglass_top',  'Document is awaiting review',           1, 1, GETUTCDATE()),
    (NEWID(), 'In Progress',      '#3b82f6', 'sync',           'Document is being actively reviewed',   2, 1, GETUTCDATE()),
    (NEWID(), 'Approved',         '#10b981', 'check_circle',   'Document has been approved',            3, 1, GETUTCDATE()),
    (NEWID(), 'Rejected',         '#f43f5e', 'cancel',         'Document has been rejected',            4, 1, GETUTCDATE()),
    (NEWID(), 'Under Revision',   '#f59e0b', 'edit_note',      'Document returned for revision',        5, 1, GETUTCDATE());
END
GO

-- 016_ApprovalWorkflowStep_StructureAssignment.sql
-- Add structure-based assignment to approval workflow steps

ALTER TABLE [ApprovalWorkflowSteps] ADD [ApproverStructureId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [ApprovalWorkflowSteps] ADD [AssignToManager] BIT NOT NULL DEFAULT 0;

CREATE INDEX IX_ApprovalWorkflowSteps_ApproverStructureId ON ApprovalWorkflowSteps(ApproverStructureId);

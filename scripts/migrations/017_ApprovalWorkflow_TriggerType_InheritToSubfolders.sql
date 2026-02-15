-- Migration: Add TriggerType and InheritToSubfolders to ApprovalWorkflows
-- Enables auto-trigger workflows on document upload

ALTER TABLE [ApprovalWorkflows] ADD [TriggerType] NVARCHAR(50) NOT NULL DEFAULT 'Manual';
ALTER TABLE [ApprovalWorkflows] ADD [InheritToSubfolders] BIT NOT NULL DEFAULT 1;

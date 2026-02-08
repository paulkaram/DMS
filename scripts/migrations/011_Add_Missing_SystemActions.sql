-- =============================================
-- Migration 011: Add Missing System Actions
-- Adds all context menu actions to SystemActions table
-- for proper role-based permission control
-- =============================================

-- =============================================
-- Cabinet Actions (Extended)
-- =============================================
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('cabinet.open', 'Open Cabinet', 'Open cabinet in new tab', 'Cabinet', 4),
('cabinet.rename', 'Rename Cabinet', 'Rename cabinet', 'Cabinet', 5),
('cabinet.settings', 'Cabinet Settings', 'Access cabinet settings and configuration', 'Cabinet', 6),
('cabinet.dashboard', 'Cabinet Dashboard', 'View cabinet dashboard and statistics', 'Cabinet', 7),
('cabinet.export', 'Export Cabinet', 'Export cabinet contents', 'Cabinet', 8);

-- =============================================
-- Folder Actions (Extended)
-- =============================================
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('folder.open', 'Open Folder', 'Open folder in new tab', 'Folder', 5),
('folder.rename', 'Rename Folder', 'Rename folder', 'Folder', 6),
('folder.upload', 'Upload to Folder', 'Upload files to folder', 'Folder', 7),
('folder.paste', 'Paste in Folder', 'Paste copied/cut items into folder', 'Folder', 8),
('folder.link.manage', 'Link Folders', 'Create links to other folders', 'Folder', 9),
('folder.link.view', 'View Folder Links', 'View links to other folders', 'Folder', 10),
('folder.share', 'Share Folder', 'Share folder with other users', 'Folder', 11),
('folder.dashboard', 'Folder Dashboard', 'View folder dashboard and statistics', 'Folder', 12),
('folder.export', 'Export Folder', 'Export folder contents', 'Folder', 13),
('folder.filingplan.add', 'Add Filing Plan', 'Add filing plan to folder', 'Folder', 14),
('folder.pattern.add', 'Add Pattern', 'Add pattern to folder', 'Folder', 15),
('folder.contenttype.manage', 'Manage Folder Content Types', 'Manage content types for folder', 'Folder', 16),
('folder.settings', 'Folder Settings', 'Access folder settings and configuration', 'Folder', 17);

-- =============================================
-- Document Actions (Extended)
-- =============================================
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('document.view', 'View Document', 'View document properties and details', 'Document', 7),
('document.preview', 'Preview Document', 'Preview document content', 'Document', 8),
('document.discard-checkout', 'Discard Checkout', 'Discard document checkout and revert changes', 'Document', 9),
('document.comments.view', 'View Comments', 'View document comments', 'Document', 10),
('document.comments.edit', 'Edit Comments', 'Edit document comments', 'Document', 11),
('document.link.manage', 'Link Documents', 'Create links to other documents', 'Document', 12),
('document.link.view', 'View Document Links', 'View links to other documents', 'Document', 13),
('document.attachments.view', 'View Attachments', 'View document attachments', 'Document', 14),
('document.attachments.edit', 'Edit Attachments', 'Edit document attachments', 'Document', 15),
('document.email', 'Send by Email', 'Send document by email', 'Document', 16),
('document.favorite', 'Toggle Favorite', 'Add or remove document from favorites', 'Document', 17),
('document.password', 'Manage Password', 'Manage document password protection', 'Document', 18),
('document.permissions', 'Document Permissions', 'Manage document-level permissions', 'Document', 19),
('document.version.view', 'Version History', 'View document version history', 'Document', 20),
('document.copy', 'Copy Document', 'Copy document to clipboard', 'Document', 21),
('document.cut', 'Cut Document', 'Cut document to clipboard', 'Document', 22),
('document.move', 'Move Document', 'Move document to another location', 'Document', 23),
('document.route', 'Route Document', 'Route document to users', 'Document', 24),
('document.workflow', 'Start Workflow', 'Start workflow on document', 'Document', 25),
('document.duplicate', 'Duplicate Document', 'Create a duplicate of the document', 'Document', 26);

-- =============================================
-- Folder/Cabinet Permissions (for permission matrix)
-- =============================================
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('cabinet.permissions', 'Cabinet Permissions', 'Manage cabinet-level permissions', 'Permissions', 4),
('folder.permissions', 'Folder Permissions', 'Manage folder-level permissions', 'Permissions', 5);

-- =============================================
-- Audit Actions (Extended for nodes)
-- =============================================
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('audit.cabinet', 'Cabinet Audit Trail', 'View audit trail for cabinets', 'Audit', 3),
('audit.folder', 'Folder Audit Trail', 'View audit trail for folders', 'Audit', 4),
('audit.document', 'Document Audit Trail', 'View audit trail for documents', 'Audit', 5);

-- =============================================
-- Grant new permissions to Administrator role
-- =============================================
DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Administrator');

IF @AdminRoleId IS NOT NULL
BEGIN
    -- Grant all new actions to Administrator
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @AdminRoleId, Id FROM SystemActions
    WHERE Code IN (
        -- Cabinet
        'cabinet.open', 'cabinet.rename', 'cabinet.settings', 'cabinet.dashboard', 'cabinet.export',
        -- Folder
        'folder.open', 'folder.rename', 'folder.upload', 'folder.paste', 'folder.link.manage',
        'folder.link.view', 'folder.share', 'folder.dashboard', 'folder.export', 'folder.filingplan.add',
        'folder.pattern.add', 'folder.contenttype.manage', 'folder.settings',
        -- Document
        'document.view', 'document.preview', 'document.discard-checkout', 'document.comments.view',
        'document.comments.edit', 'document.link.manage', 'document.link.view', 'document.attachments.view',
        'document.attachments.edit', 'document.email', 'document.favorite', 'document.password',
        'document.permissions', 'document.version.view', 'document.copy', 'document.cut', 'document.move',
        'document.route', 'document.workflow', 'document.duplicate',
        -- Permissions
        'cabinet.permissions', 'folder.permissions',
        -- Audit
        'audit.cabinet', 'audit.folder', 'audit.document'
    )
    AND Id NOT IN (SELECT ActionId FROM RoleActionPermissions WHERE RoleId = @AdminRoleId);
END

-- =============================================
-- Grant appropriate permissions to Records role
-- =============================================
DECLARE @RecordsRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Records');

IF @RecordsRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @RecordsRoleId, Id FROM SystemActions
    WHERE Code IN (
        -- Cabinet/Folder read actions
        'cabinet.open', 'cabinet.dashboard', 'folder.open', 'folder.link.view', 'folder.dashboard',
        -- Document actions for records management
        'document.view', 'document.preview', 'document.comments.view', 'document.comments.edit',
        'document.link.view', 'document.attachments.view', 'document.version.view',
        'document.copy', 'document.move', 'document.route', 'document.workflow',
        -- Audit
        'audit.document'
    )
    AND Id NOT IN (SELECT ActionId FROM RoleActionPermissions WHERE RoleId = @RecordsRoleId);
END

-- =============================================
-- Grant appropriate permissions to Auditor role
-- =============================================
DECLARE @AuditorRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Auditor');

IF @AuditorRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @AuditorRoleId, Id FROM SystemActions
    WHERE Code IN (
        -- Read-only access
        'cabinet.open', 'cabinet.dashboard', 'folder.open', 'folder.link.view', 'folder.dashboard',
        'document.view', 'document.preview', 'document.comments.view', 'document.link.view',
        'document.attachments.view', 'document.version.view',
        -- Audit trail access
        'audit.cabinet', 'audit.folder', 'audit.document'
    )
    AND Id NOT IN (SELECT ActionId FROM RoleActionPermissions WHERE RoleId = @AuditorRoleId);
END

-- =============================================
-- Grant appropriate permissions to User role
-- =============================================
DECLARE @UserRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'User');

IF @UserRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @UserRoleId, Id FROM SystemActions
    WHERE Code IN (
        -- Basic navigation
        'cabinet.open', 'folder.open', 'folder.link.view',
        -- Document basic actions
        'document.view', 'document.preview', 'document.comments.view', 'document.comments.edit',
        'document.link.view', 'document.attachments.view', 'document.email', 'document.favorite',
        'document.version.view', 'document.copy'
    )
    AND Id NOT IN (SELECT ActionId FROM RoleActionPermissions WHERE RoleId = @UserRoleId);
END

PRINT 'Migration 011: Added missing system actions successfully';
PRINT 'Total new actions added: 43';

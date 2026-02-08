-- =============================================
-- Role Permission Matrix
-- Defines what actions each role can perform
-- =============================================

-- System Actions table - defines all available actions in the system
CREATE TABLE SystemActions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(100) NOT NULL UNIQUE,           -- e.g., 'cabinet.create', 'user.manage'
    Name NVARCHAR(255) NOT NULL,                   -- Display name
    Description NVARCHAR(MAX) NULL,
    Category NVARCHAR(100) NOT NULL,               -- e.g., 'Cabinet', 'User', 'Document', 'System'
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_SystemActions_Code ON SystemActions(Code);
CREATE INDEX IX_SystemActions_Category ON SystemActions(Category);

-- Role Action Permissions - maps roles to allowed actions
CREATE TABLE RoleActionPermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RoleId UNIQUEIDENTIFIER NOT NULL,
    ActionId UNIQUEIDENTIFIER NOT NULL,
    IsAllowed BIT NOT NULL DEFAULT 1,
    GrantedBy UNIQUEIDENTIFIER NULL,
    GrantedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_RoleActionPermissions_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RoleActionPermissions_Action FOREIGN KEY (ActionId) REFERENCES SystemActions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_RoleActionPermissions UNIQUE (RoleId, ActionId)
);

CREATE INDEX IX_RoleActionPermissions_RoleId ON RoleActionPermissions(RoleId);
CREATE INDEX IX_RoleActionPermissions_ActionId ON RoleActionPermissions(ActionId);

-- =============================================
-- Seed System Actions
-- =============================================

-- Cabinet Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('cabinet.create', 'Create Cabinet', 'Create new cabinets in the system', 'Cabinet', 1),
('cabinet.delete', 'Delete Cabinet', 'Delete cabinets from the system', 'Cabinet', 2),
('cabinet.manage', 'Manage Cabinets', 'Update cabinet properties and settings', 'Cabinet', 3);

-- Folder Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('folder.create', 'Create Folder', 'Create new folders', 'Folder', 1),
('folder.delete', 'Delete Folder', 'Delete folders', 'Folder', 2),
('folder.manage', 'Manage Folders', 'Update folder properties', 'Folder', 3),
('folder.template.manage', 'Manage Folder Templates', 'Create and manage folder structure templates', 'Folder', 4);

-- Document Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('document.upload', 'Upload Documents', 'Upload new documents', 'Document', 1),
('document.delete', 'Delete Documents', 'Delete documents', 'Document', 2),
('document.checkout', 'Checkout Documents', 'Check out documents for editing', 'Document', 3),
('document.checkin', 'Checkin Documents', 'Check in documents after editing', 'Document', 4),
('document.download', 'Download Documents', 'Download document files', 'Document', 5),
('document.share', 'Share Documents', 'Share documents with other users', 'Document', 6);

-- User Management Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('user.view', 'View Users', 'View user list and details', 'User Management', 1),
('user.manage', 'Manage Users', 'Create and update users', 'User Management', 2),
('user.role.assign', 'Assign Roles', 'Assign and remove roles from users', 'User Management', 3);

-- Permission Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('permission.view', 'View Permissions', 'View permissions on nodes', 'Permissions', 1),
('permission.manage', 'Manage Permissions', 'Grant and revoke permissions', 'Permissions', 2),
('permission.inherit.break', 'Break Inheritance', 'Break permission inheritance on nodes', 'Permissions', 3);

-- Records Management Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('retention.view', 'View Retention Policies', 'View retention policies', 'Records Management', 1),
('retention.manage', 'Manage Retention Policies', 'Create and manage retention policies', 'Records Management', 2),
('disposal.view', 'View Disposals', 'View pending and completed disposals', 'Records Management', 3),
('disposal.execute', 'Execute Disposal', 'Execute document disposal', 'Records Management', 4);

-- Audit Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('audit.view', 'View Audit Trail', 'View audit logs and activity history', 'Audit', 1),
('audit.export', 'Export Audit Trail', 'Export audit logs', 'Audit', 2);

-- System Administration Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('system.settings', 'System Settings', 'Access and modify system settings', 'System', 1),
('system.contenttype.manage', 'Manage Content Types', 'Create and manage content type definitions', 'System', 2),
('system.pattern.manage', 'Manage Patterns', 'Create and manage file patterns', 'System', 3),
('system.workflow.manage', 'Manage Workflows', 'Create and manage approval workflows', 'System', 4),
('system.structure.manage', 'Manage Org Structure', 'Manage organizational structures', 'System', 5),
('system.role.manage', 'Manage Roles', 'Create and manage roles', 'System', 6);

-- Reports Actions
INSERT INTO SystemActions (Code, Name, Description, Category, SortOrder) VALUES
('report.view', 'View Reports', 'View system reports and statistics', 'Reports', 1),
('report.export', 'Export Reports', 'Export reports to various formats', 'Reports', 2);

-- =============================================
-- Seed Default Role Permissions
-- =============================================

-- Get Role IDs
DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Administrator');
DECLARE @RecordsRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Records');
DECLARE @AuditorRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Auditor');
DECLARE @UserRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'User');

-- Administrator gets ALL permissions
INSERT INTO RoleActionPermissions (RoleId, ActionId)
SELECT @AdminRoleId, Id FROM SystemActions WHERE @AdminRoleId IS NOT NULL;

-- Records Manager permissions
IF @RecordsRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @RecordsRoleId, Id FROM SystemActions
    WHERE Code IN (
        'folder.create', 'folder.manage', 'folder.template.manage',
        'document.upload', 'document.checkout', 'document.checkin', 'document.download',
        'retention.view', 'retention.manage', 'disposal.view', 'disposal.execute',
        'audit.view', 'report.view'
    );
END

-- Auditor permissions (read-only audit access)
IF @AuditorRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @AuditorRoleId, Id FROM SystemActions
    WHERE Code IN (
        'document.download',
        'audit.view', 'audit.export',
        'report.view', 'report.export',
        'retention.view', 'disposal.view'
    );
END

-- Basic User permissions
IF @UserRoleId IS NOT NULL
BEGIN
    INSERT INTO RoleActionPermissions (RoleId, ActionId)
    SELECT @UserRoleId, Id FROM SystemActions
    WHERE Code IN (
        'folder.create',
        'document.upload', 'document.checkout', 'document.checkin', 'document.download', 'document.share',
        'permission.view'
    );
END

PRINT 'Role Permission Matrix created successfully';

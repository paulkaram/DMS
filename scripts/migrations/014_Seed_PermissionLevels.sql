-- Migration 014: Seed default PermissionLevelDefinitions
-- These match the PermissionLevel enum: Read=1, Write=2, Delete=4, Admin=8, Full=15

IF NOT EXISTS (SELECT 1 FROM PermissionLevelDefinitions WHERE IsSystem = 1)
BEGIN
    INSERT INTO PermissionLevelDefinitions (Id, Name, Description, Level, Color, Icon, CanRead, CanWrite, CanDelete, CanAdmin, CanShare, CanExport, IsSystem, IsActive, SortOrder, CreatedBy, CreatedAt)
    VALUES
      (NEWID(), 'Read',   'View documents and folders',              1,  '#3B82F6', 'visibility',          1, 0, 0, 0, 0, 0, 1, 1, 1, '00000000-0000-0000-0000-000000000000', GETUTCDATE()),
      (NEWID(), 'Write',  'Edit, check-out and check-in documents',  2,  '#10B981', 'edit',                0, 1, 0, 0, 0, 0, 1, 1, 2, '00000000-0000-0000-0000-000000000000', GETUTCDATE()),
      (NEWID(), 'Delete', 'Delete documents and folders',             4,  '#F59E0B', 'delete',              0, 0, 1, 0, 0, 0, 1, 1, 3, '00000000-0000-0000-0000-000000000000', GETUTCDATE()),
      (NEWID(), 'Admin',  'Manage permissions on nodes',              8,  '#8B5CF6', 'admin_panel_settings', 0, 0, 0, 1, 0, 0, 1, 1, 4, '00000000-0000-0000-0000-000000000000', GETUTCDATE()),
      (NEWID(), 'Full',   'Full access - all permissions combined',   15, '#EF4444', 'shield',              1, 1, 1, 1, 1, 1, 1, 1, 5, '00000000-0000-0000-0000-000000000000', GETUTCDATE());
END
GO

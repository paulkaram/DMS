-- Migration 013: Add AccessMode to Folders
-- AccessMode: 0 = Normal (default), 1 = Private (users only see own documents unless Admin)

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Folders') AND name = 'AccessMode'
)
BEGIN
    ALTER TABLE Folders ADD AccessMode TINYINT NOT NULL DEFAULT 0;
END
GO

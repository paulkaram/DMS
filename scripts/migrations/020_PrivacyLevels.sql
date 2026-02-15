-- Privacy Levels feature
-- Adds hierarchical privacy classification system for folders

-- Create PrivacyLevels table
CREATE TABLE PrivacyLevels (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(256) NOT NULL,
    [Level] INT NOT NULL,
    Color NVARCHAR(32) NULL,
    [Description] NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_PrivacyLevels_Level UNIQUE ([Level])
);

-- Add PrivacyLevelId to Folders
ALTER TABLE Folders ADD PrivacyLevelId UNIQUEIDENTIFIER NULL;

ALTER TABLE Folders ADD CONSTRAINT FK_Folders_PrivacyLevels
    FOREIGN KEY (PrivacyLevelId) REFERENCES PrivacyLevels(Id);

-- Add PrivacyLevel to Users (max level the user can access)
ALTER TABLE Users ADD PrivacyLevel INT NOT NULL DEFAULT 0;

-- Insert default privacy levels
INSERT INTO PrivacyLevels (Id, Name, [Level], Color, [Description], IsActive, CreatedAt) VALUES
    (NEWID(), 'Public',       0, '#10b981', 'Accessible to all users',                    1, GETUTCDATE()),
    (NEWID(), 'Internal',     1, '#3b82f6', 'Internal use only',                           1, GETUTCDATE()),
    (NEWID(), 'Confidential', 2, '#f59e0b', 'Restricted to authorized personnel',          1, GETUTCDATE()),
    (NEWID(), 'Secret',       3, '#ef4444', 'Highly restricted, top-level clearance only',  1, GETUTCDATE());

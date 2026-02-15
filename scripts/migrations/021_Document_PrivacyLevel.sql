-- Add PrivacyLevelId to Documents
-- Allows per-document privacy classification independent of folder privacy
-- If set, document's privacy level is checked in addition to folder's privacy level

ALTER TABLE Documents ADD PrivacyLevelId UNIQUEIDENTIFIER NULL;

ALTER TABLE Documents ADD CONSTRAINT FK_Documents_PrivacyLevels
    FOREIGN KEY (PrivacyLevelId) REFERENCES PrivacyLevels(Id)
    ON DELETE SET NULL;

-- Filtered index for performance (only index non-null values)
CREATE NONCLUSTERED INDEX IX_Documents_PrivacyLevelId
    ON Documents (PrivacyLevelId)
    WHERE PrivacyLevelId IS NOT NULL;

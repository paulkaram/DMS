-- Add DraftExpiryDate, DraftExpiryDateChanged, DraftPrivacyLevelId, DraftPrivacyLevelChanged
-- to DocumentWorkingCopies for draft editing of expiry date and privacy level

ALTER TABLE DocumentWorkingCopies ADD DraftExpiryDate DATETIME2 NULL;
ALTER TABLE DocumentWorkingCopies ADD DraftExpiryDateChanged BIT NOT NULL DEFAULT 0;
ALTER TABLE DocumentWorkingCopies ADD DraftPrivacyLevelId UNIQUEIDENTIFIER NULL;
ALTER TABLE DocumentWorkingCopies ADD DraftPrivacyLevelChanged BIT NOT NULL DEFAULT 0;

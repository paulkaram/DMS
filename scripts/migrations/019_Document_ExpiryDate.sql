-- 019: Add ExpiryDate column to Documents table
-- Supports optional expiry dates for time-limited documents (contracts, licenses, permits)
-- Attachments inherit the parent document's expiry — no separate expiry per attachment

ALTER TABLE Documents ADD ExpiryDate DATETIME2 NULL;
GO

SET QUOTED_IDENTIFIER ON;
CREATE INDEX IX_Documents_ExpiryDate ON Documents(ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO

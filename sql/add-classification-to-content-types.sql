-- Add DefaultClassificationId to ContentTypeDefinitions
-- Links content types to classifications for NCAR-compliant auto-classification on upload

ALTER TABLE ContentTypeDefinitions
ADD DefaultClassificationId UNIQUEIDENTIFIER NULL;

-- FK with SET NULL (if classification deleted, content type keeps working)
ALTER TABLE ContentTypeDefinitions
ADD CONSTRAINT FK_ContentTypeDefinitions_Classifications_DefaultClassificationId
    FOREIGN KEY (DefaultClassificationId)
    REFERENCES Classifications(Id)
    ON DELETE SET NULL;

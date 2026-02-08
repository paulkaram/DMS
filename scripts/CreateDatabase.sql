-- DMS Database Creation Script
-- Run this script to create the database and tables

-- Create Database (uncomment if needed)
-- CREATE DATABASE DMS;
-- GO
-- USE DMS;
-- GO

-- Cabinets Table
CREATE TABLE Cabinets (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    BreakInheritance BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Folders Table
CREATE TABLE Folders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CabinetId UNIQUEIDENTIFIER NOT NULL REFERENCES Cabinets(Id),
    ParentFolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Path NVARCHAR(MAX),
    BreakInheritance BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Classifications Table
CREATE TABLE Classifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Language NVARCHAR(10),
    SortOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Importances Table
CREATE TABLE Importances (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Level INT DEFAULT 0,
    Color NVARCHAR(50),
    Language NVARCHAR(10),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- DocumentTypes Table
CREATE TABLE DocumentTypes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Language NVARCHAR(10),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Documents Table
CREATE TABLE Documents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FolderId UNIQUEIDENTIFIER NOT NULL REFERENCES Folders(Id),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Extension NVARCHAR(50),
    ContentType NVARCHAR(255),
    Size BIGINT DEFAULT 0,
    StoragePath NVARCHAR(MAX),
    CurrentVersion INT DEFAULT 1,
    IsCheckedOut BIT DEFAULT 0,
    CheckedOutBy UNIQUEIDENTIFIER,
    CheckedOutAt DATETIME2,
    ClassificationId UNIQUEIDENTIFIER REFERENCES Classifications(Id),
    ImportanceId UNIQUEIDENTIFIER REFERENCES Importances(Id),
    DocumentTypeId UNIQUEIDENTIFIER REFERENCES DocumentTypes(Id),
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- DocumentVersions Table
CREATE TABLE DocumentVersions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    VersionNumber INT NOT NULL,
    StoragePath NVARCHAR(MAX),
    Size BIGINT DEFAULT 0,
    Comment NVARCHAR(MAX),
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Users Table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255),
    FirstName NVARCHAR(255),
    LastName NVARCHAR(255),
    DisplayName NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    LastLoginAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedAt DATETIME2
);

-- Roles Table
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- UserRoles Table
CREATE TABLE UserRoles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    RoleId UNIQUEIDENTIFIER NOT NULL REFERENCES Roles(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserRoles UNIQUE (UserId, RoleId)
);

-- Permissions Table
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NodeType INT NOT NULL, -- 1=Cabinet, 2=Folder, 3=Document
    NodeId UNIQUEIDENTIFIER NOT NULL,
    PrincipalType INT NOT NULL, -- 1=User, 2=Role
    PrincipalId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel INT NOT NULL, -- Flags: 1=Read, 2=Write, 4=Delete, 8=Admin
    IsInherited BIT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Delegations Table
CREATE TABLE Delegations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FromUserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    ToUserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    RoleId UNIQUEIDENTIFIER REFERENCES Roles(Id),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- ActivityLogs Table
CREATE TABLE ActivityLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Action NVARCHAR(100) NOT NULL,
    NodeType INT, -- 1=Cabinet, 2=Folder, 3=Document
    NodeId UNIQUEIDENTIFIER,
    NodeName NVARCHAR(255),
    Details NVARCHAR(MAX),
    UserId UNIQUEIDENTIFIER,
    UserName NVARCHAR(255),
    IpAddress NVARCHAR(50),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- ContentTypes Table (MIME types and file extensions configuration)
CREATE TABLE ContentTypes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Extension NVARCHAR(50) NOT NULL,
    MimeType NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    Icon NVARCHAR(255),
    AllowPreview BIT DEFAULT 1,
    AllowThumbnail BIT DEFAULT 1,
    MaxFileSizeMB INT DEFAULT 100,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- FolderLinks Table (for linking folders to other folders)
CREATE TABLE FolderLinks (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SourceFolderId UNIQUEIDENTIFIER NOT NULL REFERENCES Folders(Id),
    TargetFolderId UNIQUEIDENTIFIER NOT NULL REFERENCES Folders(Id),
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_FolderLinks UNIQUE (SourceFolderId, TargetFolderId)
);

-- FilingPlans Table (automatic filing rules)
CREATE TABLE FilingPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FolderId UNIQUEIDENTIFIER NOT NULL REFERENCES Folders(Id),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Pattern NVARCHAR(500), -- File name pattern
    ClassificationId UNIQUEIDENTIFIER REFERENCES Classifications(Id),
    DocumentTypeId UNIQUEIDENTIFIER REFERENCES DocumentTypes(Id),
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Favorites Table
CREATE TABLE Favorites (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    NodeType INT NOT NULL, -- 1=Cabinet, 2=Folder, 3=Document
    NodeId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Favorites UNIQUE (UserId, NodeType, NodeId)
);

-- DocumentShares Table (for sharing documents with users)
CREATE TABLE DocumentShares (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    SharedWithUserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    SharedByUserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    PermissionLevel INT DEFAULT 1, -- 1=Read, 2=Write
    ExpiresAt DATETIME2,
    Message NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- RecycleBin Table (for soft deleted items)
CREATE TABLE RecycleBin (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NodeType INT NOT NULL, -- 1=Cabinet, 2=Folder, 3=Document
    NodeId UNIQUEIDENTIFIER NOT NULL,
    NodeName NVARCHAR(255) NOT NULL,
    OriginalPath NVARCHAR(MAX),
    OriginalParentId UNIQUEIDENTIFIER,
    DeletedBy UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    DeletedAt DATETIME2 DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2, -- Auto-purge date
    Metadata NVARCHAR(MAX) -- JSON with additional info
);

-- Vacations Table (out-of-office settings)
CREATE TABLE Vacations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    DelegateToUserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Message NVARCHAR(MAX),
    AutoReply BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedAt DATETIME2
);

-- ApprovalWorkflows Table
CREATE TABLE ApprovalWorkflows (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    FolderId UNIQUEIDENTIFIER REFERENCES Folders(Id), -- Workflow applies to this folder
    RequiredApprovers INT DEFAULT 1,
    IsSequential BIT DEFAULT 0, -- Sequential or parallel approval
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- ApprovalWorkflowSteps Table
CREATE TABLE ApprovalWorkflowSteps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkflowId UNIQUEIDENTIFIER NOT NULL REFERENCES ApprovalWorkflows(Id),
    StepOrder INT NOT NULL,
    ApproverUserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    ApproverRoleId UNIQUEIDENTIFIER REFERENCES Roles(Id),
    IsRequired BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- ApprovalRequests Table
CREATE TABLE ApprovalRequests (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    WorkflowId UNIQUEIDENTIFIER REFERENCES ApprovalWorkflows(Id),
    RequestedBy UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    Status INT DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
    DueDate DATETIME2,
    Comments NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2
);

-- ApprovalActions Table (individual approver actions)
CREATE TABLE ApprovalActions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RequestId UNIQUEIDENTIFIER NOT NULL REFERENCES ApprovalRequests(Id),
    StepId UNIQUEIDENTIFIER REFERENCES ApprovalWorkflowSteps(Id),
    ApproverId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    Action INT NOT NULL, -- 1=Approved, 2=Rejected, 3=Returned for revision
    Comments NVARCHAR(MAX),
    ActionDate DATETIME2 DEFAULT GETUTCDATE()
);

-- Lookups Table
CREATE TABLE Lookups (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- LookupItems Table
CREATE TABLE LookupItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LookupId UNIQUEIDENTIFIER NOT NULL REFERENCES Lookups(Id),
    Value NVARCHAR(255) NOT NULL,
    DisplayText NVARCHAR(500),
    Language NVARCHAR(10),
    SortOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

-- Create Indexes
CREATE INDEX IX_Folders_CabinetId ON Folders(CabinetId);
CREATE INDEX IX_Folders_ParentFolderId ON Folders(ParentFolderId);
CREATE INDEX IX_Documents_FolderId ON Documents(FolderId);
CREATE INDEX IX_Documents_CheckedOutBy ON Documents(CheckedOutBy) WHERE IsCheckedOut = 1;
CREATE INDEX IX_DocumentVersions_DocumentId ON DocumentVersions(DocumentId);
CREATE INDEX IX_Permissions_NodeId ON Permissions(NodeId);
CREATE INDEX IX_Permissions_PrincipalId ON Permissions(PrincipalId);
CREATE INDEX IX_ActivityLogs_NodeId ON ActivityLogs(NodeId);
CREATE INDEX IX_ActivityLogs_UserId ON ActivityLogs(UserId);
CREATE INDEX IX_ActivityLogs_CreatedAt ON ActivityLogs(CreatedAt DESC);
CREATE INDEX IX_Favorites_UserId ON Favorites(UserId);
CREATE INDEX IX_DocumentShares_DocumentId ON DocumentShares(DocumentId);
CREATE INDEX IX_DocumentShares_SharedWithUserId ON DocumentShares(SharedWithUserId);
CREATE INDEX IX_RecycleBin_DeletedBy ON RecycleBin(DeletedBy);
CREATE INDEX IX_RecycleBin_DeletedAt ON RecycleBin(DeletedAt DESC);
CREATE INDEX IX_Vacations_UserId ON Vacations(UserId);
CREATE INDEX IX_ApprovalRequests_DocumentId ON ApprovalRequests(DocumentId);
CREATE INDEX IX_ApprovalRequests_RequestedBy ON ApprovalRequests(RequestedBy);
CREATE INDEX IX_ApprovalRequests_Status ON ApprovalRequests(Status);
CREATE INDEX IX_ApprovalActions_RequestId ON ApprovalActions(RequestId);
CREATE INDEX IX_ApprovalActions_ApproverId ON ApprovalActions(ApproverId);
CREATE INDEX IX_ContentTypes_Extension ON ContentTypes(Extension);
CREATE INDEX IX_FolderLinks_SourceFolderId ON FolderLinks(SourceFolderId);
CREATE INDEX IX_FolderLinks_TargetFolderId ON FolderLinks(TargetFolderId);
CREATE INDEX IX_FilingPlans_FolderId ON FilingPlans(FolderId);

-- Content Type Definitions (Document Schema with Custom Fields)
CREATE TABLE ContentTypeDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Icon NVARCHAR(255),
    Color NVARCHAR(50),
    AllowOnFolders BIT DEFAULT 1,
    AllowOnDocuments BIT DEFAULT 1,
    IsRequired BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Content Type Fields (Form Builder Fields)
CREATE TABLE ContentTypeFields (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ContentTypeId UNIQUEIDENTIFIER NOT NULL REFERENCES ContentTypeDefinitions(Id),
    FieldName NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    FieldType NVARCHAR(50) NOT NULL DEFAULT 'Text', -- Text, TextArea, Number, Decimal, Date, DateTime, Boolean, Dropdown, MultiSelect, User, Lookup
    IsRequired BIT DEFAULT 0,
    IsReadOnly BIT DEFAULT 0,
    ShowInList BIT DEFAULT 0,
    IsSearchable BIT DEFAULT 1,
    DefaultValue NVARCHAR(MAX),
    ValidationRules NVARCHAR(MAX), -- JSON: {min, max, pattern, maxLength}
    LookupName NVARCHAR(100), -- Reference to Lookup table
    Options NVARCHAR(MAX), -- JSON array: [{"value": "v1", "label": "Label 1"}]
    SortOrder INT DEFAULT 0,
    GroupName NVARCHAR(100),
    ColumnSpan INT DEFAULT 12, -- 1-12 grid columns
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Document Metadata (Actual field values for documents)
CREATE TABLE DocumentMetadata (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    ContentTypeId UNIQUEIDENTIFIER NOT NULL REFERENCES ContentTypeDefinitions(Id),
    FieldId UNIQUEIDENTIFIER NOT NULL REFERENCES ContentTypeFields(Id),
    FieldName NVARCHAR(100) NOT NULL,
    Value NVARCHAR(MAX),
    NumericValue DECIMAL(18,6),
    DateValue DATETIME2,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Folder Content Type Assignments (Apply content types to folders)
CREATE TABLE FolderContentTypeAssignments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FolderId UNIQUEIDENTIFIER NOT NULL REFERENCES Folders(Id),
    ContentTypeId UNIQUEIDENTIFIER NOT NULL REFERENCES ContentTypeDefinitions(Id),
    IsRequired BIT DEFAULT 0,
    InheritToChildren BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Folder_ContentType UNIQUE (FolderId, ContentTypeId)
);

-- Indexes for Content Type tables
CREATE INDEX IX_ContentTypeFields_ContentTypeId ON ContentTypeFields(ContentTypeId);
CREATE INDEX IX_DocumentMetadata_DocumentId ON DocumentMetadata(DocumentId);
CREATE INDEX IX_DocumentMetadata_ContentTypeId ON DocumentMetadata(ContentTypeId);
CREATE INDEX IX_DocumentMetadata_FieldId ON DocumentMetadata(FieldId);
CREATE INDEX IX_FolderContentTypeAssignments_FolderId ON FolderContentTypeAssignments(FolderId);
CREATE INDEX IX_FolderContentTypeAssignments_ContentTypeId ON FolderContentTypeAssignments(ContentTypeId);

-- =============================================
-- Patterns (for document matching and auto-filing)
-- =============================================
CREATE TABLE Patterns (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Regex NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    PatternType NVARCHAR(50) DEFAULT 'Naming', -- Naming, Filing, Validation, Search
    TargetFolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    ContentTypeId UNIQUEIDENTIFIER REFERENCES ContentTypeDefinitions(Id),
    ClassificationId UNIQUEIDENTIFIER REFERENCES Classifications(Id),
    DocumentTypeId UNIQUEIDENTIFIER REFERENCES DocumentTypes(Id),
    Priority INT DEFAULT 100,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

CREATE INDEX IX_Patterns_PatternType ON Patterns(PatternType);
CREATE INDEX IX_Patterns_TargetFolderId ON Patterns(TargetFolderId);
CREATE INDEX IX_Patterns_Priority ON Patterns(Priority);

-- =============================================
-- Retention Policies (document lifecycle management)
-- =============================================
CREATE TABLE RetentionPolicies (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    RetentionDays INT DEFAULT 0, -- 0 = permanent
    ExpirationAction NVARCHAR(50) DEFAULT 'Review', -- Archive, Delete, Review, Notify
    NotifyBeforeExpiration BIT DEFAULT 1,
    NotificationDays INT DEFAULT 30,
    FolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    ClassificationId UNIQUEIDENTIFIER REFERENCES Classifications(Id),
    DocumentTypeId UNIQUEIDENTIFIER REFERENCES DocumentTypes(Id),
    RequiresApproval BIT DEFAULT 1,
    InheritToSubfolders BIT DEFAULT 1,
    IsLegalHold BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

CREATE INDEX IX_RetentionPolicies_FolderId ON RetentionPolicies(FolderId);
CREATE INDEX IX_RetentionPolicies_ClassificationId ON RetentionPolicies(ClassificationId);
CREATE INDEX IX_RetentionPolicies_DocumentTypeId ON RetentionPolicies(DocumentTypeId);

-- Document Retention tracking
CREATE TABLE DocumentRetentions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    PolicyId UNIQUEIDENTIFIER NOT NULL REFERENCES RetentionPolicies(Id),
    RetentionStartDate DATETIME2 NOT NULL,
    ExpirationDate DATETIME2,
    Status NVARCHAR(50) DEFAULT 'Active', -- Active, PendingReview, Approved, Archived, Deleted, OnHold
    NotificationSent BIT DEFAULT 0,
    ActionDate DATETIME2,
    ApprovedBy UNIQUEIDENTIFIER,
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedAt DATETIME2
);

CREATE INDEX IX_DocumentRetentions_DocumentId ON DocumentRetentions(DocumentId);
CREATE INDEX IX_DocumentRetentions_PolicyId ON DocumentRetentions(PolicyId);
CREATE INDEX IX_DocumentRetentions_Status ON DocumentRetentions(Status);
CREATE INDEX IX_DocumentRetentions_ExpirationDate ON DocumentRetentions(ExpirationDate);

-- =============================================
-- Admin Configuration Tables
-- =============================================

-- Bookmarks (template placeholders)
CREATE TABLE Bookmarks (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Placeholder NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    DefaultValue NVARCHAR(MAX),
    DataType NVARCHAR(50) DEFAULT 'Text',
    LookupName NVARCHAR(100),
    IsSystem BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Cases (document grouping)
CREATE TABLE Cases (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CaseNumber NVARCHAR(100) NOT NULL UNIQUE,
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    Status NVARCHAR(50) DEFAULT 'Open',
    Priority NVARCHAR(50),
    AssignedToUserId UNIQUEIDENTIFIER,
    FolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    DueDate DATETIME2,
    ClosedDate DATETIME2,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

CREATE INDEX IX_Cases_Status ON Cases(Status);
CREATE INDEX IX_Cases_AssignedToUserId ON Cases(AssignedToUserId);
CREATE INDEX IX_Cases_FolderId ON Cases(FolderId);

-- Endpoints (external services)
CREATE TABLE Endpoints (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Url NVARCHAR(2000) NOT NULL,
    Description NVARCHAR(MAX),
    EndpointType NVARCHAR(50) DEFAULT 'REST',
    AuthType NVARCHAR(50),
    AuthConfig NVARCHAR(MAX),
    TimeoutSeconds INT DEFAULT 30,
    RetryCount INT DEFAULT 3,
    Headers NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    LastHealthCheck DATETIME2,
    LastHealthStatus NVARCHAR(50),
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Export Configs
CREATE TABLE ExportConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    ExportFormat NVARCHAR(50) DEFAULT 'PDF',
    IncludeMetadata BIT DEFAULT 1,
    IncludeVersions BIT DEFAULT 0,
    IncludeAuditTrail BIT DEFAULT 0,
    FlattenFolders BIT DEFAULT 0,
    NamingPattern NVARCHAR(500),
    WatermarkText NVARCHAR(200),
    MaxFileSizeMB INT,
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Naming Conventions
CREATE TABLE NamingConventions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Pattern NVARCHAR(500) NOT NULL,
    AppliesTo NVARCHAR(50) DEFAULT 'Document',
    FolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    DocumentTypeId UNIQUEIDENTIFIER REFERENCES DocumentTypes(Id),
    IsRequired BIT DEFAULT 0,
    AutoGenerate BIT DEFAULT 0,
    Separator NVARCHAR(10) DEFAULT '-',
    IsActive BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

CREATE INDEX IX_NamingConventions_FolderId ON NamingConventions(FolderId);
CREATE INDEX IX_NamingConventions_DocumentTypeId ON NamingConventions(DocumentTypeId);

-- Organization Templates
CREATE TABLE OrganizationTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Structure NVARCHAR(MAX) DEFAULT '[]',
    DefaultPermissions NVARCHAR(MAX),
    IncludeContentTypes BIT DEFAULT 0,
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Permission Level Definitions
CREATE TABLE PermissionLevelDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Level INT NOT NULL UNIQUE,
    Color NVARCHAR(50),
    Icon NVARCHAR(50),
    CanRead BIT DEFAULT 1,
    CanWrite BIT DEFAULT 0,
    CanDelete BIT DEFAULT 0,
    CanAdmin BIT DEFAULT 0,
    CanShare BIT DEFAULT 0,
    CanExport BIT DEFAULT 0,
    IsSystem BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Purposes (compliance tracking)
CREATE TABLE Purposes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    PurposeType NVARCHAR(50) DEFAULT 'General',
    RequiresJustification BIT DEFAULT 0,
    RequiresApproval BIT DEFAULT 0,
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Scan Configs (OCR settings)
CREATE TABLE ScanConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Resolution INT DEFAULT 300,
    ColorMode NVARCHAR(50) DEFAULT 'Color',
    OutputFormat NVARCHAR(50) DEFAULT 'PDF',
    EnableOCR BIT DEFAULT 1,
    OcrLanguage NVARCHAR(50) DEFAULT 'eng',
    AutoDeskew BIT DEFAULT 1,
    AutoCrop BIT DEFAULT 1,
    RemoveBlankPages BIT DEFAULT 0,
    CompressionQuality INT DEFAULT 85,
    TargetFolderId UNIQUEIDENTIFIER REFERENCES Folders(Id),
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

-- Search Configs (saved searches)
CREATE TABLE SearchConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    SearchType NVARCHAR(50) DEFAULT 'FullText',
    DefaultFields NVARCHAR(MAX),
    Filters NVARCHAR(MAX),
    IncludeContent BIT DEFAULT 1,
    IncludeMetadata BIT DEFAULT 1,
    IncludeVersions BIT DEFAULT 0,
    FuzzyMatch BIT DEFAULT 0,
    MaxResults INT DEFAULT 100,
    SortField NVARCHAR(100),
    SortDirection NVARCHAR(10) DEFAULT 'desc',
    IsGlobal BIT DEFAULT 0,
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedBy UNIQUEIDENTIFIER,
    ModifiedAt DATETIME2
);

CREATE INDEX IX_SearchConfigs_IsGlobal ON SearchConfigs(IsGlobal);
CREATE INDEX IX_SearchConfigs_CreatedBy ON SearchConfigs(CreatedBy);

-- Insert default data
INSERT INTO Roles (Id, Name, Description) VALUES
    (NEWID(), 'Administrator', 'Full system access'),
    (NEWID(), 'User', 'Standard user access'),
    (NEWID(), 'Viewer', 'Read-only access');

INSERT INTO Classifications (Id, Name, Description, SortOrder) VALUES
    (NEWID(), 'General', 'General documents', 1),
    (NEWID(), 'Confidential', 'Confidential documents', 2),
    (NEWID(), 'Internal', 'Internal use only', 3),
    (NEWID(), 'Public', 'Public documents', 4);

INSERT INTO Importances (Id, Name, Level, Color) VALUES
    (NEWID(), 'Low', 1, '#28a745'),
    (NEWID(), 'Medium', 2, '#ffc107'),
    (NEWID(), 'High', 3, '#fd7e14'),
    (NEWID(), 'Critical', 4, '#dc3545');

INSERT INTO DocumentTypes (Id, Name, Description) VALUES
    (NEWID(), 'Contract', 'Legal contracts and agreements'),
    (NEWID(), 'Invoice', 'Financial invoices'),
    (NEWID(), 'Report', 'Reports and analysis'),
    (NEWID(), 'Policy', 'Company policies'),
    (NEWID(), 'Procedure', 'Standard operating procedures'),
    (NEWID(), 'Other', 'Other document types');

INSERT INTO ContentTypes (Id, Extension, MimeType, DisplayName, AllowPreview, AllowThumbnail, MaxFileSizeMB) VALUES
    (NEWID(), '.pdf', 'application/pdf', 'PDF Document', 1, 1, 100),
    (NEWID(), '.doc', 'application/msword', 'Word Document', 1, 1, 50),
    (NEWID(), '.docx', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'Word Document', 1, 1, 50),
    (NEWID(), '.xls', 'application/vnd.ms-excel', 'Excel Spreadsheet', 1, 1, 50),
    (NEWID(), '.xlsx', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Excel Spreadsheet', 1, 1, 50),
    (NEWID(), '.ppt', 'application/vnd.ms-powerpoint', 'PowerPoint Presentation', 1, 1, 100),
    (NEWID(), '.pptx', 'application/vnd.openxmlformats-officedocument.presentationml.presentation', 'PowerPoint Presentation', 1, 1, 100),
    (NEWID(), '.txt', 'text/plain', 'Text File', 1, 0, 10),
    (NEWID(), '.csv', 'text/csv', 'CSV File', 1, 0, 50),
    (NEWID(), '.jpg', 'image/jpeg', 'JPEG Image', 1, 1, 20),
    (NEWID(), '.jpeg', 'image/jpeg', 'JPEG Image', 1, 1, 20),
    (NEWID(), '.png', 'image/png', 'PNG Image', 1, 1, 20),
    (NEWID(), '.gif', 'image/gif', 'GIF Image', 1, 1, 10),
    (NEWID(), '.bmp', 'image/bmp', 'Bitmap Image', 1, 1, 20),
    (NEWID(), '.tiff', 'image/tiff', 'TIFF Image', 1, 1, 50),
    (NEWID(), '.zip', 'application/zip', 'ZIP Archive', 0, 0, 500),
    (NEWID(), '.rar', 'application/x-rar-compressed', 'RAR Archive', 0, 0, 500),
    (NEWID(), '.mp4', 'video/mp4', 'MP4 Video', 1, 1, 2000),
    (NEWID(), '.mp3', 'audio/mpeg', 'MP3 Audio', 1, 0, 100),
    (NEWID(), '.xml', 'application/xml', 'XML File', 1, 0, 10),
    (NEWID(), '.json', 'application/json', 'JSON File', 1, 0, 10),
    (NEWID(), '.html', 'text/html', 'HTML File', 1, 0, 10);

-- Insert demo user (password: demo123 - for development only)
DECLARE @DemoUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @AdminRoleId UNIQUEIDENTIFIER;

SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

INSERT INTO Users (Id, Username, Email, FirstName, LastName, DisplayName, IsActive, CreatedAt) VALUES
    (@DemoUserId, 'demo', 'demo@example.com', 'Demo', 'User', 'Demo User', 1, GETUTCDATE()),
    (NEWID(), 'admin', 'admin@example.com', 'System', 'Administrator', 'System Administrator', 1, GETUTCDATE()),
    (NEWID(), 'john.doe', 'john.doe@example.com', 'John', 'Doe', 'John Doe', 1, GETUTCDATE()),
    (NEWID(), 'jane.smith', 'jane.smith@example.com', 'Jane', 'Smith', 'Jane Smith', 1, GETUTCDATE());

-- Assign admin role to demo user
INSERT INTO UserRoles (Id, UserId, RoleId) VALUES
    (NEWID(), @DemoUserId, @AdminRoleId);

-- Create sample cabinet and folder structure
DECLARE @CabinetId UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderId1 UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderId2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Cabinets (Id, Name, Description, CreatedBy, CreatedAt) VALUES
    (@CabinetId, 'General Documents', 'Main document cabinet for general files', @DemoUserId, GETUTCDATE());

INSERT INTO Folders (Id, CabinetId, ParentFolderId, Name, Description, Path, CreatedBy, CreatedAt) VALUES
    (@FolderId1, @CabinetId, NULL, 'Contracts', 'Legal contracts and agreements', '/Contracts', @DemoUserId, GETUTCDATE()),
    (@FolderId2, @CabinetId, NULL, 'Reports', 'Monthly and annual reports', '/Reports', @DemoUserId, GETUTCDATE()),
    (NEWID(), @CabinetId, @FolderId1, '2024', 'Contracts from 2024', '/Contracts/2024', @DemoUserId, GETUTCDATE()),
    (NEWID(), @CabinetId, @FolderId2, 'Monthly', 'Monthly reports', '/Reports/Monthly', @DemoUserId, GETUTCDATE());

-- Grant permissions on the cabinet to demo user
INSERT INTO Permissions (Id, NodeType, NodeId, PrincipalType, PrincipalId, PermissionLevel, IsInherited, CreatedBy, CreatedAt) VALUES
    (NEWID(), 1, @CabinetId, 1, @DemoUserId, 15, 0, @DemoUserId, GETUTCDATE()); -- Full access (1+2+4+8=15)

PRINT 'DMS Database created successfully!';
PRINT 'Demo user created: username=demo, password=demo123';

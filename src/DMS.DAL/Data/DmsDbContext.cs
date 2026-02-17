using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Data;

public class DmsDbContext : DbContext
{
    public DmsDbContext(DbContextOptions<DmsDbContext> options) : base(options) { }

    // Core entities
    public DbSet<Cabinet> Cabinets => Set<Cabinet>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    // Users & Roles
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // Permissions
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<EffectivePermission> EffectivePermissions => Set<EffectivePermission>();
    public DbSet<PermissionDelegation> PermissionDelegations => Set<PermissionDelegation>();

    // Organization Structure
    public DbSet<Structure> Structures => Set<Structure>();
    public DbSet<StructureMember> StructureMembers => Set<StructureMember>();

    // Reference Data
    public DbSet<PrivacyLevel> PrivacyLevels => Set<PrivacyLevel>();
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<Importance> Importances => Set<Importance>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Lookup> Lookups => Set<Lookup>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();
    public DbSet<ContentType> ContentTypes => Set<ContentType>();
    public DbSet<FileType> FileTypes => Set<FileType>();

    // Content Type Definitions (Form Builder)
    public DbSet<ContentTypeDefinition> ContentTypeDefinitions => Set<ContentTypeDefinition>();
    public DbSet<ContentTypeField> ContentTypeFields => Set<ContentTypeField>();
    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();
    public DbSet<DocumentVersionMetadata> DocumentVersionMetadata => Set<DocumentVersionMetadata>();
    public DbSet<FolderContentTypeAssignment> FolderContentTypeAssignments => Set<FolderContentTypeAssignment>();
    public DbSet<CabinetContentTypeAssignment> CabinetContentTypeAssignments => Set<CabinetContentTypeAssignment>();

    // Document Features
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();
    public DbSet<DocumentAttachment> DocumentAttachments => Set<DocumentAttachment>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<DocumentPassword> DocumentPasswords => Set<DocumentPassword>();
    public DbSet<DocumentShare> DocumentShares => Set<DocumentShare>();
    public DbSet<DocumentShortcut> DocumentShortcuts => Set<DocumentShortcut>();
    public DbSet<DocumentAnnotation> DocumentAnnotations => Set<DocumentAnnotation>();
    public DbSet<SavedSignature> SavedSignatures => Set<SavedSignature>();
    public DbSet<DocumentWorkingCopy> DocumentWorkingCopies => Set<DocumentWorkingCopy>();

    // Folders & Links
    public DbSet<FolderLink> FolderLinks => Set<FolderLink>();
    public DbSet<FolderTemplate> FolderTemplates => Set<FolderTemplate>();
    public DbSet<FolderTemplateNode> FolderTemplateNodes => Set<FolderTemplateNode>();
    public DbSet<FolderTemplateUsage> FolderTemplateUsages => Set<FolderTemplateUsage>();

    // Favorites & Recycle Bin
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<RecycleBinItem> RecycleBinItems => Set<RecycleBinItem>();

    // Approval Workflows
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps => Set<ApprovalWorkflowStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();

    // Retention & Compliance
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<DocumentRetention> DocumentRetentions => Set<DocumentRetention>();
    public DbSet<RetentionTriggerEvent> RetentionTriggerEvents => Set<RetentionTriggerEvent>();
    public DbSet<RetentionTriggerLog> RetentionTriggerLogs => Set<RetentionTriggerLog>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<LegalHoldDocument> LegalHoldDocuments => Set<LegalHoldDocument>();
    public DbSet<DisposalCertificate> DisposalCertificates => Set<DisposalCertificate>();
    public DbSet<DisposalRequest> DisposalRequests => Set<DisposalRequest>();
    public DbSet<DisposalRequestDocument> DisposalRequestDocuments => Set<DisposalRequestDocument>();
    public DbSet<DisposalApproval> DisposalApprovals => Set<DisposalApproval>();
    public DbSet<IntegrityVerificationLog> IntegrityVerificationLogs => Set<IntegrityVerificationLog>();
    public DbSet<PreservationMetadata> PreservationMetadata => Set<PreservationMetadata>();

    // State Machine
    public DbSet<StateTransitionRule> StateTransitionRules => Set<StateTransitionRule>();
    public DbSet<StateTransitionLog> StateTransitionLogs => Set<StateTransitionLog>();

    // Physical Archive
    public DbSet<PhysicalLocation> PhysicalLocations => Set<PhysicalLocation>();
    public DbSet<PhysicalItem> PhysicalItems => Set<PhysicalItem>();
    public DbSet<AccessionRequest> AccessionRequests => Set<AccessionRequest>();
    public DbSet<AccessionRequestItem> AccessionRequestItems => Set<AccessionRequestItem>();
    public DbSet<CirculationRecord> CirculationRecords => Set<CirculationRecord>();
    public DbSet<CustodyTransfer> CustodyTransfers => Set<CustodyTransfer>();

    // Filing & Patterns
    public DbSet<FilingPlan> FilingPlans => Set<FilingPlan>();
    public DbSet<Pattern> Patterns => Set<Pattern>();

    // Admin Configuration
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<ServiceEndpoint> ServiceEndpoints => Set<ServiceEndpoint>();
    public DbSet<ExportConfig> ExportConfigs => Set<ExportConfig>();
    public DbSet<NamingConvention> NamingConventions => Set<NamingConvention>();
    public DbSet<OrganizationTemplate> OrganizationTemplates => Set<OrganizationTemplate>();
    public DbSet<PermissionLevelDefinition> PermissionLevelDefinitions => Set<PermissionLevelDefinition>();
    public DbSet<Purpose> Purposes => Set<Purpose>();
    public DbSet<ScanConfig> ScanConfigs => Set<ScanConfig>();
    public DbSet<SearchConfig> SearchConfigs => Set<SearchConfig>();

    // Security & Encryption
    public DbSet<EncryptionKeyStore> EncryptionKeyStore => Set<EncryptionKeyStore>();
    public DbSet<AccessReviewCampaign> AccessReviewCampaigns => Set<AccessReviewCampaign>();
    public DbSet<AccessReviewEntry> AccessReviewEntries => Set<AccessReviewEntry>();

    // Search
    public DbSet<SearchIndexQueue> SearchIndexQueue => Set<SearchIndexQueue>();

    // Background Job Tracking
    public DbSet<BackgroundJobExecution> BackgroundJobExecutions => Set<BackgroundJobExecution>();

    // Role Permissions
    public DbSet<SystemAction> SystemActions => Set<SystemAction>();
    public DbSet<RoleActionPermission> RoleActionPermissions => Set<RoleActionPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DmsDbContext).Assembly);

        // Ignore query projection classes (not mapped to tables)
        modelBuilder.Ignore<DocumentWithNames>();

        // Audit entities live in separate AuditDbContext (DMS_Audit database)
        modelBuilder.Ignore<ActivityLog>();
        modelBuilder.Ignore<PermissionAuditLog>();

        // Global query filter for soft-deletable entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
                var filter = System.Linq.Expressions.Expression.Lambda(property, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditable>();
        var now = DateTime.Now;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
                // Don't overwrite CreatedAt/CreatedBy on update
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
            }
        }
    }
}

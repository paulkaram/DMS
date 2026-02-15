using DMS.DAL.Data;
using DMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, string connectionString)
    {
        // Register EF Core DbContext
        services.AddDbContext<DmsDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // Core repositories
        services.AddScoped<ICabinetRepository, CabinetRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

        // Reference data repositories
        services.AddScoped<IClassificationRepository, ClassificationRepository>();
        services.AddScoped<IImportanceRepository, ImportanceRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        // User repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Dashboard repository
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // Additional feature repositories
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IDocumentShareRepository, DocumentShareRepository>();
        services.AddScoped<IRecycleBinRepository, RecycleBinRepository>();

        services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();

        // Content and folder management repositories
        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<IFolderLinkRepository, FolderLinkRepository>();
        services.AddScoped<IDocumentShortcutRepository, DocumentShortcutRepository>();
        services.AddScoped<IFilingPlanRepository, FilingPlanRepository>();

        // Content Type Definitions (Form Builder)
        services.AddScoped<IContentTypeDefinitionRepository, ContentTypeDefinitionRepository>();

        // Patterns and Retention Policies
        services.AddScoped<IPatternRepository, PatternRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();

        // Document Features Repositories
        services.AddScoped<IDocumentCommentRepository, DocumentCommentRepository>();
        services.AddScoped<IDocumentAttachmentRepository, DocumentAttachmentRepository>();
        services.AddScoped<IDocumentLinkRepository, DocumentLinkRepository>();
        services.AddScoped<IDocumentPasswordRepository, DocumentPasswordRepository>();

        // Admin Configuration Repositories
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<IEndpointRepository, EndpointRepository>();
        services.AddScoped<IExportConfigRepository, ExportConfigRepository>();
        services.AddScoped<INamingConventionRepository, NamingConventionRepository>();
        services.AddScoped<IOrganizationTemplateRepository, OrganizationTemplateRepository>();
        services.AddScoped<IPermissionLevelDefinitionRepository, PermissionLevelDefinitionRepository>();
        services.AddScoped<IPurposeRepository, PurposeRepository>();
        services.AddScoped<IScanConfigRepository, ScanConfigRepository>();
        services.AddScoped<ISearchConfigRepository, SearchConfigRepository>();

        // ISO Compliance Repositories (ISO 15489, 27001, 14721)
        services.AddScoped<IIntegrityVerificationLogRepository, IntegrityVerificationLogRepository>();
        services.AddScoped<ILegalHoldRepository, LegalHoldRepository>();
        services.AddScoped<ILegalHoldDocumentRepository, LegalHoldDocumentRepository>();
        services.AddScoped<IDisposalCertificateRepository, DisposalCertificateRepository>();

        // ISO 15489 Checkout System Repositories
        services.AddScoped<IDocumentVersionMetadataRepository, DocumentVersionMetadataRepository>();
        services.AddScoped<IDocumentWorkingCopyRepository, DocumentWorkingCopyRepository>();

        // Document Annotations
        services.AddScoped<IDocumentAnnotationRepository, DocumentAnnotationRepository>();
        services.AddScoped<ISavedSignatureRepository, SavedSignatureRepository>();

        // Enterprise Permission System Repositories
        services.AddScoped<IStructureRepository, StructureRepository>();
        services.AddScoped<IEffectivePermissionRepository, EffectivePermissionRepository>();
        services.AddScoped<IPermissionAuditRepository, PermissionAuditRepository>();
        services.AddScoped<IPermissionDelegationRepository, PermissionDelegationRepository>();

        // Folder Structure Templates
        services.AddScoped<IFolderTemplateRepository, FolderTemplateRepository>();

        // Role Permission Matrix
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

        // Workflow Statuses
        services.AddScoped<IWorkflowStatusRepository, WorkflowStatusRepository>();

        // Privacy Levels
        services.AddScoped<IPrivacyLevelRepository, PrivacyLevelRepository>();

        return services;
    }
}

using DMS.BL.Interfaces;
using DMS.BL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.BL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<ICabinetService, CabinetService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        // User and auth services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        // Permission service
        services.AddScoped<IPermissionService, PermissionService>();

        // Reference data service
        services.AddScoped<IReferenceDataService, ReferenceDataService>();

        // Dashboard service
        services.AddScoped<IDashboardService, DashboardService>();

        // Additional feature services
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IRecycleBinService, RecycleBinService>();

        services.AddScoped<IApprovalService, ApprovalService>();

        // Document feature services
        services.AddScoped<IDocumentCommentService, DocumentCommentService>();
        services.AddScoped<IDocumentAttachmentService, DocumentAttachmentService>();
        services.AddScoped<IDocumentLinkService, DocumentLinkService>();
        services.AddScoped<IDocumentPasswordService, DocumentPasswordService>();

        // Email and notification services
        services.AddScoped<IEmailService, EmailService>();

        // Bulk operation services
        services.AddScoped<IBulkOperationService, BulkOperationService>();

        // Preview service
        services.AddScoped<IPreviewService, PreviewService>();

        // ISO Compliance Services (ISO 15489, 27001, 14721)
        services.AddScoped<IIntegrityService, IntegrityService>();
        services.AddScoped<ILegalHoldService, LegalHoldService>();
        services.AddScoped<IDisposalService, DisposalService>();

        // Document State Machine (NCAR governance)
        services.AddScoped<IDocumentStateService, DocumentStateService>();

        // Retention Engine (event-based triggers, legal hold suspension)
        services.AddScoped<IRetentionEngineService, RetentionEngineService>();

        // Security Hardening
        services.AddScoped<IKeyManagementService, KeyManagementService>();
        services.AddScoped<IAccessReviewService, AccessReviewService>();

        // Physical Archive Module
        services.AddScoped<IPhysicalLocationService, PhysicalLocationService>();
        services.AddScoped<IPhysicalItemService, PhysicalItemService>();
        services.AddScoped<IAccessionService, AccessionService>();
        services.AddScoped<ICirculationService, CirculationService>();
        services.AddScoped<ICustodyService, CustodyService>();

        // Enterprise Permission System Services
        services.AddScoped<IStructureService, StructureService>();

        // Security Services
        services.AddScoped<IFileValidationService, FileValidationService>();

        // Folder Structure Templates
        services.AddScoped<IFolderTemplateService, FolderTemplateService>();

        // Role Permission Matrix
        services.AddScoped<IRolePermissionService, RolePermissionService>();

        // Scan Service
        services.AddScoped<IScanService, ScanService>();

        // Document Shortcuts
        services.AddScoped<IDocumentShortcutService, DocumentShortcutService>();

        // Document Annotations
        services.AddScoped<IDocumentAnnotationService, DocumentAnnotationService>();
        services.AddScoped<ISavedSignatureService, SavedSignatureService>();

        // PDF Page Management
        services.AddScoped<IPdfPageService, PdfPageService>();

        // Reports
        services.AddScoped<IReportsService, ReportsService>();

        // Workflow Statuses
        services.AddScoped<IWorkflowStatusService, WorkflowStatusService>();

        // Security Services (NCA ECC compliance)
        services.AddScoped<IWatermarkService, WatermarkService>();

        // Archival & Preservation (ISO 14721 OAIS)
        services.AddScoped<IArchivalExportService, ArchivalExportService>();

        // Search (SQL fallback - OpenSearch registered conditionally in Program.cs)
        services.AddScoped<ISearchService, SqlSearchService>();

        // Preservation (ISO 14721 OAIS format management)
        services.AddScoped<IPreservationService, PreservationService>();

        // System Health & Monitoring
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // Retention Governance Dashboard
        services.AddScoped<IRetentionDashboardService, RetentionDashboardService>();

        return services;
    }
}

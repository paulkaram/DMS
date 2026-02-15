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

        return services;
    }
}

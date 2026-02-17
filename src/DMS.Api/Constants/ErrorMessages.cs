namespace DMS.Api.Constants;

public static class ErrorMessages
{
    // Generic
    public const string FileRequired = "File is required";
    public const string NoDocumentsSpecified = "No documents specified";
    public const string CabinetIdRequired = "CabinetId is required";
    public const string InvalidManifestJson = "Invalid manifest JSON";
    public const string ManifestRequiresPage = "Manifest must contain at least one page";
    public const string AtLeastOneImageRequired = "At least one image is required";
    public const string NoSystemDefaultContentType = "No system default content type is configured";
    public const string FailedToSetSystemDefault = "Failed to set system default";
    public const string DocumentPasswordRequired = "Document is password protected. Provide the password via X-Document-Password header.";
    public const string NoFileUploaded = "No file uploaded";
    public const string UnableToSubmitAction = "Unable to submit action";
    public const string FailedToSetPassword = "Failed to set password";
    public const string InvalidPasswordOrChangeFailed = "Invalid current password or failed to change password";

    public static class Permissions
    {
        // Cabinet permissions
        public const string ViewCabinet = "You don't have permission to view this cabinet";
        public const string CreateCabinet = "You don't have permission to create cabinets";
        public const string ManageCabinet = "You don't have permission to manage cabinets";
        public const string UpdateCabinet = "You don't have permission to update this cabinet";
        public const string DeleteCabinet = "You don't have permission to delete cabinets";
        public const string DeleteThisCabinet = "You don't have permission to delete this cabinet";
        public const string CreateFoldersInCabinet = "You don't have permission to create folders in this cabinet";
        public const string MoveFoldersToCabinet = "You don't have permission to move folders to this cabinet";

        // Folder permissions
        public const string ViewFolder = "You don't have permission to view this folder";
        public const string CreateFoldersHere = "You don't have permission to create folders here";
        public const string EditFolder = "You don't have permission to edit this folder";
        public const string MoveFolder = "You don't have permission to move this folder";
        public const string MoveFoldersToDestination = "You don't have permission to move folders to this destination";
        public const string DeleteFolder = "You don't have permission to delete this folder";
        public const string UploadToFolder = "You don't have permission to upload to this folder";
        public const string UploadDocumentsToFolder = "You don't have permission to upload documents to this folder";

        // Document permissions
        public const string ViewDocument = "You don't have permission to view this document";
        public const string DownloadDocument = "You don't have permission to download this document";
        public const string EditDocument = "You don't have permission to edit this document";
        public const string CheckOutDocument = "You don't have permission to check out this document";
        public const string CheckInDocument = "You don't have permission to check in this document";
        public const string DiscardCheckout = "You don't have permission to discard this checkout";
        public const string MoveDocument = "You don't have permission to move this document";
        public const string MoveDocumentsToFolder = "You don't have permission to move documents to this folder";
        public const string CopyDocument = "You don't have permission to copy this document";
        public const string CopyDocumentsToFolder = "You don't have permission to copy documents to this folder";
        public const string DeleteDocument = "You don't have permission to delete this document";
        public const string PreviewDocument = "You don't have permission to preview this document";
        public const string RestoreVersion = "You don't have permission to restore versions of this document";
        public const string ViewWorkingCopy = "You don't have permission to view this working copy";
        public const string DocumentPendingApproval = "This document is pending approval and not accessible";
        public const string DocumentExpired = "This document has expired and is no longer accessible";
        public const string InsufficientPrivacyLevel = "You don't have sufficient privacy clearance to access this content";

        // Shortcut permissions
        public const string ReadDocument = "You don't have permission to read this document";
        public const string WriteToFolder = "You don't have permission to write to this folder";

        // Annotation permissions
        public const string ViewAnnotations = "You don't have permission to view this document";
        public const string WriteAnnotations = "You don't have write permission on this document";
        public const string WritePages = "You don't have write permission on this document";
        public const string ReadPages = "You don't have permission to view this document";
        public const string TransitionDocument = "You don't have permission to change document lifecycle state";
    }

    // Role-based permission keys
    public static class PermissionKeys
    {
        public const string CabinetCreate = "cabinet.create";
        public const string CabinetManage = "cabinet.manage";
        public const string CabinetDelete = "cabinet.delete";
    }
}

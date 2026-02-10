namespace DMS.BL.Constants;

public static class DomainConstants
{
    public const int DefaultRetentionDays = 2555; // ~7 years
    public const int DefaultDpi = 150;
    public const double PointsPerInch = 72.0;
    public const int MaxHierarchyDepth = 50;
    public const int JwtExpirationHours = 24;
    public const int OcrPreviewMaxLength = 500;
    public const int CommentLogMaxLength = 100;
    public const int TextPreviewMaxBytes = 524_288; // 512KB
    public const int InitialMajorVersion = 1;
    public const int InitialMinorVersion = 0;
    public const string InitialVersionLabel = "1.0";
    public const string HashAlgorithm = "SHA256";
    public const int DefaultIntegrityBatchSize = 100;
    public const int IntegrityVerificationIntervalDays = 30;

    public static class VersionTypes
    {
        public const string Major = "Major";
        public const string Minor = "Minor";
    }

    public static class ContentCategories
    {
        public const string Original = "Original";
    }

    public static class DisposalMethods
    {
        public const string HardDelete = "HardDelete";
        public const string CryptographicErasure = "CryptographicErasure";
    }

    public static class VerificationTypes
    {
        public const string Manual = "Manual";
        public const string Scheduled = "Scheduled";
    }

    public static class ContentTypes
    {
        public const string Pdf = "application/pdf";
        public const string OctetStream = "application/octet-stream";
    }

    public static class PdfMetadata
    {
        public const string ScannerTitle = "Scanned Document";
        public const string ScannerCreator = "DMS Scanner";
        public const string PageManagerCreator = "DMS Page Manager";
    }
}

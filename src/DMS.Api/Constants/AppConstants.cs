namespace DMS.Api.Constants;

public static class AppConstants
{
    public const long MaxUploadSizeBytes = 200_000_000; // 200MB
    public const double StorageCapacityGB = 1024.0; // 1TB
    public const int DefaultPageSize = 50;
    public const int DefaultActivityPageSize = 100;
    public const int DefaultExpiringDaysAhead = 30;
    public const int DefaultRecentActivityTake = 10;
    public const int DefaultStaleCheckoutHours = 24;
    public const int MaxPageSize = 200;
    public const int MaxTreeResults = 5000;

    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Admin = "Admin";
        public const string Records = "Records";
        public const string Legal = "Legal";
        public const string Auditor = "Auditor";
        public const string AdminOrLegal = "Admin,Legal";
    }

    public static class NodeTypes
    {
        public const string Cabinet = "Cabinet";
        public const string Folder = "Folder";
        public const string Document = "Document";
    }
}

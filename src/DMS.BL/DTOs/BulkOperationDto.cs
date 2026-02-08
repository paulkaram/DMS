namespace DMS.BL.DTOs;

public class BulkDeleteRequest
{
    public List<Guid> DocumentIds { get; set; } = new();
}

public class BulkMoveRequest
{
    public List<Guid> DocumentIds { get; set; } = new();
    public Guid TargetFolderId { get; set; }
}

public class BulkDownloadRequest
{
    public List<Guid> DocumentIds { get; set; } = new();
}

public class BulkOperationResult
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
}

public class BulkOperationError
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

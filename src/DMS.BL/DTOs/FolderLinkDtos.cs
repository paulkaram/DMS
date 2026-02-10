namespace DMS.BL.DTOs;

public class CreateFolderLinkRequest
{
    public Guid SourceFolderId { get; set; }
    public Guid TargetFolderId { get; set; }
}

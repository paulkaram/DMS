namespace DMS.DAL.Entities;

public class Favorite
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int NodeType { get; set; } // 1=Cabinet, 2=Folder, 3=Document
    public Guid NodeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

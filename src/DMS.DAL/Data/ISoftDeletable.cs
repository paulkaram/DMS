namespace DMS.DAL.Data;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
}

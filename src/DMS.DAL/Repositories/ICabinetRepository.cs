using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface ICabinetRepository : IRepository<Cabinet>
{
    Task<IEnumerable<Cabinet>> GetActiveAsync();
    Task<IEnumerable<Cabinet>> SearchAsync(string? name);
}

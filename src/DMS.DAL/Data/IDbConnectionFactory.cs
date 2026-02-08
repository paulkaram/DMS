using System.Data;

namespace DMS.DAL.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

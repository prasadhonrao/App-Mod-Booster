using Microsoft.Data.SqlClient;

namespace ExpenseMgmt
{
    public interface IDbConnectionFactory { SqlConnection CreateConnection(); }

    public class SqlDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqlDbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not configured.");
        }
        public SqlConnection CreateConnection() => new(_connectionString);
    }
}

using Microsoft.Data.SqlClient;

namespace ExpenseMgmt
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnectionFactory dbFactory, ILogger<UserRepository> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetUsers", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<User>();
                while (await reader.ReadAsync()) list.Add(MapUser(reader));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsersAsync - UserRepository.cs");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetUserById", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapUser(reader) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByIdAsync({UserId}) - UserRepository.cs", userId);
                throw;
            }
        }

        private static User MapUser(SqlDataReader r) => new()
        {
            UserId = r.GetInt32(r.GetOrdinal("UserId")),
            UserName = r.GetString(r.GetOrdinal("UserName")),
            Email = r.GetString(r.GetOrdinal("Email")),
            RoleId = r.GetInt32(r.GetOrdinal("RoleId")),
            RoleName = r.GetString(r.GetOrdinal("RoleName")),
            ManagerId = r.IsDBNull(r.GetOrdinal("ManagerId")) ? null : r.GetInt32(r.GetOrdinal("ManagerId")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
        };
    }
}

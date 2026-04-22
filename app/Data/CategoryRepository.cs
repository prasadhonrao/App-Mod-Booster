using Microsoft.Data.SqlClient;

namespace ExpenseMgmt
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync();
        Task<IEnumerable<ExpenseStatus>> GetStatusesAsync();
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(IDbConnectionFactory dbFactory, ILogger<CategoryRepository> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetCategories", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<ExpenseCategory>();
                while (await reader.ReadAsync())
                    list.Add(new() { CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")), CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")), IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")) });
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoriesAsync - CategoryRepository.cs");
                throw;
            }
        }

        public async Task<IEnumerable<ExpenseStatus>> GetStatusesAsync()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetStatuses", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<ExpenseStatus>();
                while (await reader.ReadAsync())
                    list.Add(new() { StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")), StatusName = reader.GetString(reader.GetOrdinal("StatusName")) });
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStatusesAsync - CategoryRepository.cs");
                throw;
            }
        }
    }
}

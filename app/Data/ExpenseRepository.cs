using Microsoft.Data.SqlClient;

namespace ExpenseMgmt
{
    public interface IExpenseRepository
    {
        Task<IEnumerable<Expense>> GetAllExpensesAsync(string? statusFilter = null, int? userId = null);
        Task<Expense?> GetExpenseByIdAsync(int expenseId);
        Task<int> CreateExpenseAsync(CreateExpenseRequest request);
        Task<bool> UpdateExpenseStatusAsync(int expenseId, UpdateExpenseStatusRequest request);
        Task<bool> DeleteExpenseAsync(int expenseId);
        Task<bool> SubmitExpenseAsync(int expenseId);
    }

    public class ExpenseRepository : IExpenseRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<ExpenseRepository> _logger;

        public ExpenseRepository(IDbConnectionFactory dbFactory, ILogger<ExpenseRepository> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Expense>> GetAllExpensesAsync(string? statusFilter = null, int? userId = null)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetExpenses", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<Expense>();
                while (await reader.ReadAsync()) list.Add(MapExpense(reader));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllExpensesAsync - ExpenseRepository.cs");
                throw;
            }
        }

        public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_GetExpenseById", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapExpense(reader) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExpenseByIdAsync({ExpenseId}) - ExpenseRepository.cs", expenseId);
                throw;
            }
        }

        public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_CreateExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@UserId", request.UserId);
                cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
                cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateExpenseAsync - ExpenseRepository.cs");
                throw;
            }
        }

        public async Task<bool> UpdateExpenseStatusAsync(int expenseId, UpdateExpenseStatusRequest request)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_UpdateExpenseStatus", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                cmd.Parameters.AddWithValue("@StatusId", request.StatusId);
                cmd.Parameters.AddWithValue("@ReviewedBy", request.ReviewedBy);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateExpenseStatusAsync({ExpenseId}) - ExpenseRepository.cs", expenseId);
                throw;
            }
        }

        public async Task<bool> DeleteExpenseAsync(int expenseId)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_DeleteExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteExpenseAsync({ExpenseId}) - ExpenseRepository.cs", expenseId);
                throw;
            }
        }

        public async Task<bool> SubmitExpenseAsync(int expenseId)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                await conn.OpenAsync();
                using var cmd = new SqlCommand("sp_SubmitExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitExpenseAsync({ExpenseId}) - ExpenseRepository.cs", expenseId);
                throw;
            }
        }

        private static Expense MapExpense(SqlDataReader r) => new()
        {
            ExpenseId = r.GetInt32(r.GetOrdinal("ExpenseId")),
            UserId = r.GetInt32(r.GetOrdinal("UserId")),
            UserName = r.GetString(r.GetOrdinal("UserName")),
            CategoryId = r.GetInt32(r.GetOrdinal("CategoryId")),
            CategoryName = r.GetString(r.GetOrdinal("CategoryName")),
            StatusId = r.GetInt32(r.GetOrdinal("StatusId")),
            StatusName = r.GetString(r.GetOrdinal("StatusName")),
            AmountMinor = r.GetInt32(r.GetOrdinal("AmountMinor")),
            Currency = r.GetString(r.GetOrdinal("Currency")),
            ExpenseDate = r.GetDateTime(r.GetOrdinal("ExpenseDate")),
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
            ReceiptFile = r.IsDBNull(r.GetOrdinal("ReceiptFile")) ? null : r.GetString(r.GetOrdinal("ReceiptFile")),
            SubmittedAt = r.IsDBNull(r.GetOrdinal("SubmittedAt")) ? null : r.GetDateTime(r.GetOrdinal("SubmittedAt")),
            ReviewedBy = r.IsDBNull(r.GetOrdinal("ReviewedBy")) ? null : r.GetInt32(r.GetOrdinal("ReviewedBy")),
            ReviewedAt = r.IsDBNull(r.GetOrdinal("ReviewedAt")) ? null : r.GetDateTime(r.GetOrdinal("ReviewedAt")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
        };
    }
}

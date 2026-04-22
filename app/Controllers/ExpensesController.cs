using Microsoft.AspNetCore.Mvc;

namespace ExpenseMgmt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseRepository _expenseRepo;
        private readonly ILogger<ExpensesController> _logger;

        // Dummy data returned when DB unavailable
        private static readonly List<Expense> _dummyExpenses = new()
        {
            new() { ExpenseId = 1, UserId = 1, UserName = "Alice Example", CategoryId = 1, CategoryName = "Travel", StatusId = 2, StatusName = "Submitted", AmountMinor = 2540, Currency = "GBP", ExpenseDate = DateTime.Today.AddDays(-2), Description = "Taxi to client site", CreatedAt = DateTime.UtcNow },
            new() { ExpenseId = 2, UserId = 1, UserName = "Alice Example", CategoryId = 2, CategoryName = "Meals", StatusId = 3, StatusName = "Approved", AmountMinor = 1425, Currency = "GBP", ExpenseDate = DateTime.Today.AddDays(-10), Description = "Client lunch", CreatedAt = DateTime.UtcNow }
        };

        public ExpensesController(IExpenseRepository expenseRepo, ILogger<ExpensesController> logger)
        {
            _expenseRepo = expenseRepo;
            _logger = logger;
        }

        /// <summary>Get all expenses, optionally filtered by status or user</summary>
        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] string? status = null, [FromQuery] int? userId = null)
        {
            try
            {
                var expenses = await _expenseRepo.GetAllExpensesAsync(status, userId);
                return Ok(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/expenses - ExpensesController.cs:GetExpenses");
                return Ok(new { isDummyData = true, error = $"Database unavailable: {ex.Message} | File: ExpensesController.cs, Method: GetExpenses. If using Managed Identity, ensure the identity has db_datareader/db_datawriter roles and AZURE_CLIENT_ID is set in App Service settings.", data = _dummyExpenses });
            }
        }

        /// <summary>Get a specific expense by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpense(int id)
        {
            try
            {
                var expense = await _expenseRepo.GetExpenseByIdAsync(id);
                if (expense == null) return NotFound();
                return Ok(expense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/expenses/{Id} - ExpensesController.cs:GetExpense", id);
                return StatusCode(503, new { error = $"{ex.Message} | File: ExpensesController.cs, Method: GetExpense" });
            }
        }

        /// <summary>Create a new expense</summary>
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
        {
            try
            {
                var newId = await _expenseRepo.CreateExpenseAsync(request);
                return CreatedAtAction(nameof(GetExpense), new { id = newId }, new { expenseId = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in POST /api/expenses - ExpensesController.cs:CreateExpense");
                return StatusCode(503, new { error = $"{ex.Message} | File: ExpensesController.cs, Method: CreateExpense" });
            }
        }

        /// <summary>Update expense status (approve/reject/submit)</summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
        {
            try
            {
                var ok = await _expenseRepo.UpdateExpenseStatusAsync(id, request);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in PATCH /api/expenses/{Id}/status - ExpensesController.cs:UpdateStatus", id);
                return StatusCode(503, new { error = $"{ex.Message} | File: ExpensesController.cs, Method: UpdateStatus" });
            }
        }

        /// <summary>Submit an expense for review</summary>
        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitExpense(int id)
        {
            try
            {
                var ok = await _expenseRepo.SubmitExpenseAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in POST /api/expenses/{Id}/submit - ExpensesController.cs:SubmitExpense", id);
                return StatusCode(503, new { error = $"{ex.Message} | File: ExpensesController.cs, Method: SubmitExpense" });
            }
        }

        /// <summary>Delete an expense</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var ok = await _expenseRepo.DeleteExpenseAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in DELETE /api/expenses/{Id} - ExpensesController.cs:DeleteExpense", id);
                return StatusCode(503, new { error = $"{ex.Message} | File: ExpensesController.cs, Method: DeleteExpense" });
            }
        }
    }
}

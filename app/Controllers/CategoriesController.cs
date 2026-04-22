using Microsoft.AspNetCore.Mvc;

namespace ExpenseMgmt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _catRepo;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryRepository catRepo, ILogger<CategoriesController> logger)
        {
            _catRepo = catRepo;
            _logger = logger;
        }

        /// <summary>Get all expense categories</summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try { return Ok(await _catRepo.GetCategoriesAsync()); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/categories - CategoriesController.cs:GetCategories");
                return Ok(new { isDummyData = true, error = $"{ex.Message} | File: CategoriesController.cs, Method: GetCategories", data = new[] { new { CategoryId = 1, CategoryName = "Travel", IsActive = true }, new { CategoryId = 2, CategoryName = "Meals", IsActive = true } } });
            }
        }

        /// <summary>Get all expense statuses</summary>
        [HttpGet("statuses")]
        public async Task<IActionResult> GetStatuses()
        {
            try { return Ok(await _catRepo.GetStatusesAsync()); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/categories/statuses - CategoriesController.cs:GetStatuses");
                return Ok(new { isDummyData = true, error = $"{ex.Message} | File: CategoriesController.cs, Method: GetStatuses", data = new[] { new { StatusId = 1, StatusName = "Draft" }, new { StatusId = 2, StatusName = "Submitted" }, new { StatusId = 3, StatusName = "Approved" }, new { StatusId = 4, StatusName = "Rejected" } } });
            }
        }
    }
}

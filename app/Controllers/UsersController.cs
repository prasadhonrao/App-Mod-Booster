using Microsoft.AspNetCore.Mvc;

namespace ExpenseMgmt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ILogger<UsersController> _logger;

        private static readonly List<User> _dummyUsers = new()
        {
            new() { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
            new() { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
        };

        public UsersController(IUserRepository userRepo, ILogger<UsersController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        /// <summary>Get all users</summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                return Ok(await _userRepo.GetAllUsersAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/users - UsersController.cs:GetUsers");
                return Ok(new { isDummyData = true, error = $"{ex.Message} | File: UsersController.cs, Method: GetUsers", data = _dummyUsers });
            }
        }

        /// <summary>Get a specific user by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);
                return user == null ? NotFound() : Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB error in GET /api/users/{Id} - UsersController.cs:GetUser", id);
                return StatusCode(503, new { error = $"{ex.Message} | File: UsersController.cs, Method: GetUser" });
            }
        }
    }
}

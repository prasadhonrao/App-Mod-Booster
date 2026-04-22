using Microsoft.AspNetCore.Mvc;

namespace ExpenseMgmt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public class ChatRequest { public string Message { get; set; } = string.Empty; }

        /// <summary>Send a message to the AI assistant</summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var response = await _chatService.GetResponseAsync(request.Message);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatController.cs:SendMessage");
                return StatusCode(503, new { error = $"{ex.Message} | File: ChatController.cs, Method: SendMessage" });
            }
        }
    }
}

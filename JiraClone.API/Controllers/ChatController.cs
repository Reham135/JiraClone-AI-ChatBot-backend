using System.Security.Claims;
using JiraClone.Application.DTOs.Chat;
using JiraClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JiraClone.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] ChatRequestDto request)
        {
            try
            {
                // Get the current user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var response = await _chatService.ProcessMessageAsync(request, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your message", error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                // Get the current user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                // Validate file size (50MB max)
                if (file.Length > 50 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size exceeds 50MB limit" });
                }

                // Validate file type (images only for now)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Only image files are allowed" });
                }

                var response = await _chatService.HandleFileUploadAsync(file, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the file", error = ex.Message });
            }
        }

        [HttpPost("clear")]
        public IActionResult ClearConversation()
        {
            try
            {
                // Get the current user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                _chatService.ClearConversation(userId);
                return Ok(new { message = "Conversation cleared successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while clearing conversation", error = ex.Message });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "chat" });
        }
    }
}

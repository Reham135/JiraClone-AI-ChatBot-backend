using JiraClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JiraClone.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpPost("upload/{taskId}")]
    public async Task<IActionResult> UploadAttachment(ulong taskId, IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var attachment = await _attachmentService.UploadAttachmentAsync(taskId, file, userId);
            if (attachment == null)
            {
                return BadRequest("Failed to upload attachment");
            }

            return Ok(attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    [HttpGet("task/{taskId}")]
    public async Task<IActionResult> GetTaskAttachments(ulong taskId)
    {
        var attachments = await _attachmentService.GetTaskAttachmentsAsync(taskId);
        return Ok(attachments);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttachment(ulong id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _attachmentService.DeleteAttachmentAsync(id, userId);
        if (!result)
        {
            return NotFound("Attachment not found or you don't have permission to delete it");
        }

        return NoContent();
    }

    [HttpGet("{id}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachment(ulong id)
    {
        var fileData = await _attachmentService.GetAttachmentFileAsync(id);
        if (fileData == null)
        {
            return NotFound();
        }

        return File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.fileName);
    }
}

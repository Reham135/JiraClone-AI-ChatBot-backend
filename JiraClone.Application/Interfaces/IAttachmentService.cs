using JiraClone.Application.DTOs.Attachments;
using Microsoft.AspNetCore.Http;

namespace JiraClone.Application.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentDto?> UploadAttachmentAsync(ulong taskId, IFormFile file, ulong uploadedBy);
    Task<IEnumerable<AttachmentDto>> GetTaskAttachmentsAsync(ulong taskId);
    Task<bool> DeleteAttachmentAsync(ulong id, ulong userId);
    Task<(byte[] fileBytes, string contentType, string fileName)?> GetAttachmentFileAsync(ulong id);
}

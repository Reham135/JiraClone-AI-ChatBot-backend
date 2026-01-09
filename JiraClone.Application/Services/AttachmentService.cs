using JiraClone.Application.DTOs.Attachments;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace JiraClone.Application.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _uploadPath;
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB
    private readonly string[] _allowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedVideoTypes = { ".mp4", ".webm", ".mov", ".avi" };

    public AttachmentService(IUnitOfWork unitOfWork, string uploadPath)
    {
        _unitOfWork = unitOfWork;
        _uploadPath = uploadPath;

        // Create uploads directory if it doesn't exist
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<AttachmentDto?> UploadAttachmentAsync(ulong taskId, IFormFile file, ulong uploadedBy)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        // Validate file size
        if (file.Length > _maxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
        }

        // Validate file type
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = _allowedImageTypes.Contains(extension);
        var isVideo = _allowedVideoTypes.Contains(extension);

        if (!isImage && !isVideo)
        {
            throw new InvalidOperationException("Only image and video files are allowed");
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create attachment record
        var attachment = new Attachment
        {
            TaskId = taskId,
            FileName = file.FileName,
            FilePath = $"/uploads/{uniqueFileName}",
            FileType = file.ContentType,
            FileSize = file.Length,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.Attachments.AddAsync(attachment);

        if (!await _unitOfWork.CompleteAsync())
        {
            // Delete file if database save fails
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return null;
        }

        return await MapToDtoAsync(attachment);
    }

    public async Task<IEnumerable<AttachmentDto>> GetTaskAttachmentsAsync(ulong taskId)
    {
        var attachments = await _unitOfWork.Attachments.FindAsync(a => a.TaskId == taskId);
        var dtos = new List<AttachmentDto>();

        foreach (var attachment in attachments.OrderBy(a => a.UploadedAt))
        {
            var dto = await MapToDtoAsync(attachment);
            if (dto != null)
            {
                dtos.Add(dto);
            }
        }

        return dtos;
    }

    public async Task<bool> DeleteAttachmentAsync(ulong id, ulong userId)
    {
        var attachment = await _unitOfWork.Attachments.GetByIdAsync(id);
        if (attachment == null)
        {
            return false;
        }

        // Check if user is the uploader or has permission
        if (attachment.UploadedBy != userId)
        {
            // Could add additional permission checks here
            return false;
        }

        // Delete file from disk
        var physicalPath = Path.Combine(_uploadPath, Path.GetFileName(attachment.FilePath));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _unitOfWork.Attachments.Remove(attachment);
        return await _unitOfWork.CompleteAsync();
    }

    public async Task<(byte[] fileBytes, string contentType, string fileName)?> GetAttachmentFileAsync(ulong id)
    {
        var attachment = await _unitOfWork.Attachments.GetByIdAsync(id);
        if (attachment == null)
        {
            return null;
        }

        var physicalPath = Path.Combine(_uploadPath, Path.GetFileName(attachment.FilePath));
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        var fileBytes = await File.ReadAllBytesAsync(physicalPath);
        return (fileBytes, attachment.FileType, attachment.FileName);
    }

    private async Task<AttachmentDto?> MapToDtoAsync(Attachment attachment)
    {
        var uploader = await _unitOfWork.Users.GetByIdAsync(attachment.UploadedBy);

        return new AttachmentDto
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
            UploadedBy = attachment.UploadedBy,
            UploadedByName = uploader != null ? $"{uploader.FirstName} {uploader.LastName}" : "Unknown",
            UploadedAt = attachment.UploadedAt
        };
    }
}

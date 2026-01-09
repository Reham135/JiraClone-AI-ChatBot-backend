using System.Text.RegularExpressions;
using JiraClone.Application.DTOs.Comments;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Repositories;

namespace JiraClone.Application.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public CommentService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByTaskIdAsync(ulong taskId)
    {
        var comments = await _unitOfWork.Comments.FindAsync(c => c.TaskId == taskId);
        var commentsList = comments.OrderByDescending(c => c.CreatedAt).ToList();

        var commentDtos = new List<CommentDto>();
        foreach (var comment in commentsList)
        {
            var creator = await _unitOfWork.Users.GetByIdAsync(comment.CreatedBy);
            commentDtos.Add(MapToDto(comment, creator));
        }

        return commentDtos;
    }

    public async Task<CommentDto?> GetCommentByIdAsync(ulong id)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        if (comment == null)
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(comment.CreatedBy);
        return MapToDto(comment, creator);
    }

    public async Task<CommentDto?> CreateCommentAsync(CreateCommentDto dto, ulong userId)
    {
        // Verify task exists
        var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
        if (task == null)
        {
            return null;
        }

        //// Get Egypt timezone (Africa/Cairo)
        //var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        //var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

        var comment = new Comment
        {
            Content = dto.Content,
            TaskId = dto.TaskId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.Comments.AddAsync(comment);
        if (!added || !await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        // Detect @mentions and create notifications
        await ProcessMentionsAsync(comment, userId);

        var creator = await _unitOfWork.Users.GetByIdAsync(userId);
        return MapToDto(comment, creator);
    }

    public async Task<CommentDto?> UpdateCommentAsync(ulong id, UpdateCommentDto dto, ulong userId)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        if (comment == null || comment.CreatedBy != userId)
        {
            return null; // Not found or user doesn't own this comment
        }

        // Get Egypt timezone (Africa/Cairo)
        var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

        comment.Content = dto.Content;
        comment.UpdatedAt = egyptTime;

        _unitOfWork.Comments.Update(comment);
        if (!await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(userId);
        return MapToDto(comment, creator);
    }

    public async Task<bool> DeleteCommentAsync(ulong id, ulong userId)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        if (comment == null || comment.CreatedBy != userId)
        {
            return false; // Not found or user doesn't own this comment
        }

        _unitOfWork.Comments.Remove(comment);
        return await _unitOfWork.CompleteAsync();
    }

    private async Task ProcessMentionsAsync(Comment comment, ulong commenterId)
    {
        // Extract @mentions from comment content using regex
        var mentionPattern = @"@([^\s@.,:;!?]+(?:\s+[^\s@.,:;!?]+)*)";
        var matches = Regex.Matches(comment.Content, mentionPattern);

        if (matches.Count == 0)
        {
            return;
        }

        // Get all users to search for mentions
        var allUsers = await _unitOfWork.Users.GetAllAsync();
        var commenter = await _unitOfWork.Users.GetByIdAsync(commenterId);
        var commenterName = commenter != null ? $"{commenter.FirstName} {commenter.LastName}" : "Someone";

        foreach (Match match in matches)
        {
            var mentionedName = match.Groups[1].Value.Trim();

            // Find user by matching first name, last name, or full name
            var mentionedUser = allUsers.FirstOrDefault(u =>
                $"{u.FirstName} {u.LastName}".Equals(mentionedName, StringComparison.OrdinalIgnoreCase) ||
                u.FirstName.Equals(mentionedName, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Equals(mentionedName, StringComparison.OrdinalIgnoreCase));

            if (mentionedUser != null && mentionedUser.Id != commenterId)
            {
                // Create notification for mentioned user
                var message = $"{commenterName} mentioned you in a comment";
                await _notificationService.CreateNotificationAsync(
                    mentionedUser.Id,
                    message,
                    comment.TaskId,
                    comment.Id);
            }
        }
    }

    private CommentDto MapToDto(Comment comment, User? creator)
    {
        var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(comment.CreatedAt, egyptTimeZone);
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            TaskId = comment.TaskId,
            CreatedBy = comment.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            CreatedAt = egyptTime,
            UpdatedAt = comment.UpdatedAt
        };
    }
}

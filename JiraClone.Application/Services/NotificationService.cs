using JiraClone.Application.DTOs.Notifications;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Repositories;

namespace JiraClone.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private dynamic? _hubContext;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Set the SignalR hub context for sending real-time notifications
    /// This is set via dependency injection from Program.cs
    /// </summary>
    public void SetHubContext(object hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(ulong userId)
    {
        var notifications = (await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId)).ToList();

        // Get unique task IDs
        var taskIds = notifications
            .Where(n => n.TaskId.HasValue)
            .Select(n => n.TaskId!.Value)
            .Distinct()
            .ToList();

        // Fetch tasks to get their project IDs
        var tasks = new Dictionary<ulong, ulong>();
        foreach (var taskId in taskIds)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
            if (task != null)
            {
                tasks[taskId] = task.ProjectId;
            }
        }

     var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

return notifications
    .OrderByDescending(n => n.CreatedAt)
    .Select(n => new NotificationDto
    {
        Id = n.Id,
        Message = n.Message,
        UserId = n.UserId,
        TaskId = n.TaskId,
        CommentId = n.CommentId,
        ProjectId = n.TaskId.HasValue && tasks.ContainsKey(n.TaskId.Value)
            ? tasks[n.TaskId.Value]
            : null,
        IsRead = n.IsRead,
        CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(n.CreatedAt, egyptTimeZone)
    })
    .ToList();

    }

    public async Task<NotificationDto?> MarkAsReadAsync(ulong id, ulong userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return null;
        }

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);

        if (!await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        // Get project ID if task exists
        ulong? projectId = null;
        if (notification.TaskId.HasValue)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(notification.TaskId.Value);
            if (task != null)
            {
                projectId = task.ProjectId;
            }
        }
        var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(notification.CreatedAt, egyptTimeZone);

        return new NotificationDto
        {
            Id = notification.Id,
            Message = notification.Message,
            UserId = notification.UserId,
            TaskId = notification.TaskId,
            CommentId = notification.CommentId,
            ProjectId = projectId,
            IsRead = notification.IsRead,
            CreatedAt = egyptTime
        };
    }

    public async Task<NotificationDto?> CreateNotificationAsync(ulong userId, string message, ulong? taskId = null, ulong? commentId = null)
    {
        var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            TaskId = taskId,
            CommentId = commentId,
            IsRead = false,
            CreatedAt = egyptTime
        };

        var added = await _unitOfWork.Notifications.AddAsync(notification);
        if (!added || !await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        // Get project ID if task exists
        ulong? projectId = null;
        if (taskId.HasValue)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId.Value);
            if (task != null)
            {
                projectId = task.ProjectId;
            }
        }

        var notificationDto = new NotificationDto
        {
            Id = notification.Id,
            Message = notification.Message,
            UserId = notification.UserId,
            TaskId = notification.TaskId,
            CommentId = notification.CommentId,
            ProjectId = projectId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        // Send real-time notification via SignalR if hub context is available
        if (_hubContext != null)
        {
            try
            {
                var userGroupName = $"user_{userId}";
                await _hubContext.Clients.Group(userGroupName).SendAsync("ReceiveNotification", notificationDto);
            }
            catch (Exception)
            {
                // Silently fail if SignalR notification fails - the notification is still saved in the database
                // This ensures the system remains resilient even if real-time delivery fails
            }
        }

        return notificationDto;
    }
}

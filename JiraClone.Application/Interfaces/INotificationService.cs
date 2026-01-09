using JiraClone.Application.DTOs.Notifications;

namespace JiraClone.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(ulong userId);
    Task<NotificationDto?> MarkAsReadAsync(ulong id, ulong userId);
    Task<NotificationDto?> CreateNotificationAsync(ulong userId, string message, ulong? taskId = null, ulong? commentId = null);
    void SetHubContext(object hubContext);
}

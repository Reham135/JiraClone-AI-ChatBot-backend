namespace JiraClone.Application.DTOs.Notifications;

public class NotificationDto
{
    public ulong Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ulong UserId { get; set; }
    public ulong? TaskId { get; set; }
    public ulong? CommentId { get; set; }
    public ulong? ProjectId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace JiraClone.Domain.Entities;

public class Notification
{
    public ulong Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ulong UserId { get; set; }
    public ulong? TaskId { get; set; }
    public ulong? CommentId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual TaskItem? Task { get; set; }
    public virtual Comment? Comment { get; set; }
}

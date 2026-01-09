namespace JiraClone.Domain.Entities;

public class Comment
{
    public ulong Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ulong TaskId { get; set; }
    public ulong CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
}

using JiraClone.Domain.Enums;

namespace JiraClone.Domain.Entities;

public class TaskItem
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public Enums.TaskPriority Priority { get; set; }
    public Enums.TaskType Type { get; set; } = Enums.TaskType.Task;
    public ulong? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; } // JSON or comma-separated
    public ulong ProjectId { get; set; }
    public ulong? SprintId { get; set; }
    public ulong CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual Sprint? Sprint { get; set; }
    public virtual User? Assignee { get; set; }
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

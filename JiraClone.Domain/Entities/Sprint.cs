using JiraClone.Domain.Enums;

namespace JiraClone.Domain.Entities;

public class Sprint
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SprintStatus Status { get; set; }
    public ulong ProjectId { get; set; }
    public ulong CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Tasks;

public class TaskDto
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Domain.Enums.TaskStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public Domain.Enums.TaskPriority Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public Domain.Enums.TaskType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public ulong? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
    public int CommentCount { get; set; }
    public ulong ProjectId { get; set; }
    public ulong? SprintId { get; set; }
    public string? SprintName { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string TaskKey { get; set; } = string.Empty;
    public ulong CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

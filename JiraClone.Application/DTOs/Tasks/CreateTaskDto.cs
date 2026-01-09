using System.ComponentModel.DataAnnotations;
using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Tasks;

public class CreateTaskDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Domain.Enums.TaskStatus Status { get; set; } = Domain.Enums.TaskStatus.ToDo;

    [Required]
    public Domain.Enums.TaskPriority Priority { get; set; } = Domain.Enums.TaskPriority.Medium;

    [Required]
    public Domain.Enums.TaskType Type { get; set; } = Domain.Enums.TaskType.Task;

    public ulong? AssigneeId { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    [Required]
    public ulong ProjectId { get; set; }

    public ulong? SprintId { get; set; }
}

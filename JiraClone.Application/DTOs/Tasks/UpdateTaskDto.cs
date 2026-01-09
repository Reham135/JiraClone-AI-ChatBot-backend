using System.ComponentModel.DataAnnotations;
using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Tasks;

public class UpdateTaskDto
{
    [MinLength(3)]
    [MaxLength(300)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Domain.Enums.TaskStatus? Status { get; set; }

    public Domain.Enums.TaskPriority? Priority { get; set; }

    public Domain.Enums.TaskType? Type { get; set; }

    public ulong? AssigneeId { get; set; }

    public DateTime? DueDate { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public ulong? SprintId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Sprints;

public class CreateSprintDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Goal { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public ulong ProjectId { get; set; }

    public SprintStatus Status { get; set; } = SprintStatus.Planned;
}

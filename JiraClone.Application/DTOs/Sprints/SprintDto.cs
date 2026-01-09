using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Sprints;

public class SprintDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SprintStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public ulong ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public ulong CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

namespace JiraClone.Application.DTOs.Sprints;

/// <summary>
/// Response DTO for sprint lookup endpoint containing assignee, project, and current active sprint information
/// </summary>
public class SprintLookupResponseDto
{
    public ulong? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeEmail { get; set; }
    public ulong? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public ulong? SprintId { get; set; }
}

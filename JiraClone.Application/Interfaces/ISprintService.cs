using JiraClone.Application.DTOs.Sprints;

namespace JiraClone.Application.Interfaces;

public interface ISprintService
{
    Task<IEnumerable<SprintDto>> GetAllSprintsAsync(ulong? projectId = null);
    Task<SprintDto?> GetSprintByIdAsync(ulong id);
    Task<SprintDto?> CreateSprintAsync(CreateSprintDto dto, ulong userId);
    Task<SprintDto?> UpdateSprintAsync(ulong id, UpdateSprintDto dto, ulong userId);
    Task<bool> DeleteSprintAsync(ulong id, ulong userId);
    Task<SprintLookupResponseDto> GetSprintLookupAsync(string? assigneeName = null, string? projectName = null);
}

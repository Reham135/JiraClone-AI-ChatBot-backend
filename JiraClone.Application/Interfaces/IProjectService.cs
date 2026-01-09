using JiraClone.Application.DTOs.Projects;

namespace JiraClone.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(ulong userId);
    Task<ProjectDto?> GetProjectByIdAsync(ulong id, ulong userId);
    Task<ProjectDto?> CreateProjectAsync(CreateProjectDto dto, ulong userId);
    Task<ProjectDto?> UpdateProjectAsync(ulong id, UpdateProjectDto dto, ulong userId);
    Task<bool> DeleteProjectAsync(ulong id, ulong userId);
}

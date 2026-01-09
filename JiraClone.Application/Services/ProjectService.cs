using JiraClone.Application.DTOs.Projects;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Repositories;

namespace JiraClone.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(ulong userId)
    {
        var projects = await _unitOfWork.Projects.GetAllAsync();
        var projectsList = projects.ToList();

        var projectDtos = new List<ProjectDto>();
        foreach (var project in projectsList)
        {
            var creator = await _unitOfWork.Users.GetByIdAsync(project.CreatedBy);
            var tasks = await _unitOfWork.Tasks.FindAsync(t => t.ProjectId == project.Id);

            projectDtos.Add(new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedBy = project.CreatedBy,
                CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                TaskCount = tasks.Count()
            });
        }

        return projectDtos;
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(ulong id, ulong userId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(project.CreatedBy);
        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.ProjectId == project.Id);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = project.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            TaskCount = tasks.Count()
        };
    }

    public async Task<ProjectDto?> CreateProjectAsync(CreateProjectDto dto, ulong userId)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.Projects.AddAsync(project);
        if (!added || !await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(userId);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = project.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            TaskCount = 0
        };
    }

    public async Task<ProjectDto?> UpdateProjectAsync(ulong id, UpdateProjectDto dto, ulong userId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
        {
            return null;
        }

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Projects.Update(project);
        if (!await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(project.CreatedBy);
        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.ProjectId == project.Id);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = project.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            TaskCount = tasks.Count()
        };
    }

    public async Task<bool> DeleteProjectAsync(ulong id, ulong userId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
        {
            return false;
        }

        _unitOfWork.Projects.Remove(project);
        return await _unitOfWork.CompleteAsync();
    }
}

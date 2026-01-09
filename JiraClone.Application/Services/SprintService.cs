using JiraClone.Application.DTOs.Sprints;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Repositories;

namespace JiraClone.Application.Services;

public class SprintService : ISprintService
{
    private readonly IUnitOfWork _unitOfWork;

    public SprintService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SprintDto>> GetAllSprintsAsync(ulong? projectId = null)
    {
        var sprints = await _unitOfWork.Sprints.GetAllAsync();
        var sprintsList = sprints.AsEnumerable();

        if (projectId.HasValue)
        {
            sprintsList = sprintsList.Where(s => s.ProjectId == projectId.Value);
        }

        var sprintDtos = new List<SprintDto>();
        foreach (var sprint in sprintsList)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(sprint.ProjectId);
            var creator = await _unitOfWork.Users.GetByIdAsync(sprint.CreatedBy);
            var tasks = await _unitOfWork.Tasks.FindAsync(t => t.SprintId == sprint.Id);

            sprintDtos.Add(MapToDto(sprint, project, creator, tasks.Count()));
        }

        return sprintDtos.OrderByDescending(s => s.StartDate);
    }

    public async Task<SprintDto?> GetSprintByIdAsync(ulong id)
    {
        var sprint = await _unitOfWork.Sprints.GetByIdAsync(id);
        if (sprint == null)
        {
            return null;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(sprint.ProjectId);
        var creator = await _unitOfWork.Users.GetByIdAsync(sprint.CreatedBy);
        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.SprintId == sprint.Id);

        return MapToDto(sprint, project, creator, tasks.Count());
    }

    public async Task<SprintDto?> CreateSprintAsync(CreateSprintDto dto, ulong userId)
    {
        // Verify project exists
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId);
        if (project == null)
        {
            return null;
        }

        // Validate dates
        if (dto.EndDate <= dto.StartDate)
        {
            return null; // End date must be after start date
        }

        var sprint = new Sprint
        {
            Name = dto.Name,
            Goal = dto.Goal,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            ProjectId = dto.ProjectId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.Sprints.AddAsync(sprint);
        if (!added || !await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(userId);
        return MapToDto(sprint, project, creator, 0);
    }

    public async Task<SprintDto?> UpdateSprintAsync(ulong id, UpdateSprintDto dto, ulong userId)
    {
        var sprint = await _unitOfWork.Sprints.GetByIdAsync(id);
        if (sprint == null)
        {
            return null;
        }

        // Validate dates
        if (dto.EndDate <= dto.StartDate)
        {
            return null; // End date must be after start date
        }

        sprint.Name = dto.Name;
        sprint.Goal = dto.Goal;
        sprint.StartDate = dto.StartDate;
        sprint.EndDate = dto.EndDate;
        sprint.Status = dto.Status;
        sprint.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Sprints.Update(sprint);
        if (!await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(sprint.ProjectId);
        var creator = await _unitOfWork.Users.GetByIdAsync(sprint.CreatedBy);
        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.SprintId == sprint.Id);

        return MapToDto(sprint, project, creator, tasks.Count());
    }

    public async Task<bool> DeleteSprintAsync(ulong id, ulong userId)
    {
        var sprint = await _unitOfWork.Sprints.GetByIdAsync(id);
        if (sprint == null)
        {
            return false;
        }

        // Remove sprint association from tasks (set to null)
        var tasks = await _unitOfWork.Tasks.FindAsync(t => t.SprintId == id);
        foreach (var task in tasks)
        {
            task.SprintId = null;
            _unitOfWork.Tasks.Update(task);
        }

        _unitOfWork.Sprints.Remove(sprint);
        return await _unitOfWork.CompleteAsync();
    }

    public async Task<SprintLookupResponseDto> GetSprintLookupAsync(string? assigneeName = null, string? projectName = null)
    {
        var response = new SprintLookupResponseDto();

        // Query for assignee by name if provided
        if (!string.IsNullOrWhiteSpace(assigneeName))
        {
            var assignee = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                (u.FirstName + " " + u.LastName).ToLower() == assigneeName.ToLower() ||
                (u.FirstName).Contains(assigneeName.ToLower()) ||
                (u.Email != null && u.Email.ToLower().Contains(assigneeName.ToLower())));

            if (assignee != null)
            {
                response.AssigneeId = assignee.Id;
                response.AssigneeName = $"{assignee.FirstName} {assignee.LastName}";
                response.AssigneeEmail = $"{assignee.Email}";
            }
        }

        // Query for project by name if provided
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            var project = await _unitOfWork.Projects.FirstOrDefaultAsync(p =>
                p.Name.ToLower().Contains(projectName.ToLower()));

            if (project != null)
            {
                response.ProjectId = project.Id;
                response.ProjectName = project.Name;

                // Find the current active sprint for this project
                var activeSprints = await _unitOfWork.Sprints.FindAsync(s =>
                    s.ProjectId == project.Id &&
                    s.Status == Domain.Enums.SprintStatus.Active);

                // Get the first active sprint (there should typically be only one active sprint per project)
                var currentSprint = activeSprints.OrderBy(s => s.StartDate).FirstOrDefault();

                if (currentSprint != null)
                {
                    response.SprintId = currentSprint.Id;
                }
            }
        }
        else if (response.AssigneeId.HasValue)
        {
            // If no project name is provided but we have an assignee, try to find any active sprint
            var activeSprints = await _unitOfWork.Sprints.FindAsync(s =>
                s.Status == Domain.Enums.SprintStatus.Active);

            // Get the first active sprint ordered by start date (most recent)
            var currentSprint = activeSprints.OrderByDescending(s => s.StartDate).FirstOrDefault();

            if (currentSprint != null)
            {
                response.SprintId = currentSprint.Id;
            }
        }

        return response;
    }

    private SprintDto MapToDto(Sprint sprint, Project? project, User? creator, int taskCount)
    {
        return new SprintDto
        {
            Id = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Status = sprint.Status,
            StatusName = sprint.Status.ToString(),
            ProjectId = sprint.ProjectId,
            ProjectName = project?.Name ?? "Unknown",
            CreatedBy = sprint.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            TaskCount = taskCount,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt

        };
    }
}

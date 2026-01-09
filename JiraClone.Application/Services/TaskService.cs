using JiraClone.Application.DTOs.Tasks;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Entities;
using JiraClone.Domain.Enums;
using JiraClone.Infrastructure.Repositories;

namespace JiraClone.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;

    public TaskService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TaskDto>> GetAllTasksAsync(
        ulong? projectId = null,
        Domain.Enums.TaskStatus? status = null,
        ulong? assigneeId = null,
        Domain.Enums.TaskPriority? priority = null,
        ulong? sprintId = null)
    {
        var tasks = await _unitOfWork.Tasks.GetAllAsync();

        // Apply filters
        var filteredTasks = tasks.AsEnumerable();

        if (projectId.HasValue)
            filteredTasks = filteredTasks.Where(t => t.ProjectId == projectId.Value);

        if (status.HasValue)
            filteredTasks = filteredTasks.Where(t => t.Status == status.Value);

        if (assigneeId.HasValue)
            filteredTasks = filteredTasks.Where(t => t.AssigneeId == assigneeId.Value);

        if (priority.HasValue)
            filteredTasks = filteredTasks.Where(t => t.Priority == priority.Value);

        if (sprintId.HasValue)
            filteredTasks = filteredTasks.Where(t => t.SprintId == sprintId.Value);

        var taskDtos = new List<TaskDto>();
        foreach (var task in filteredTasks)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);
            var creator = await _unitOfWork.Users.GetByIdAsync(task.CreatedBy);
            var assignee = task.AssigneeId.HasValue ? await _unitOfWork.Users.GetByIdAsync(task.AssigneeId.Value) : null;

            taskDtos.Add(MapToDto(task, project, creator, assignee));
        }

        return taskDtos;
    }

    public async Task<TaskDto?> GetTaskByIdAsync(ulong id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return null;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);
        var creator = await _unitOfWork.Users.GetByIdAsync(task.CreatedBy);
        var assignee = task.AssigneeId.HasValue ? await _unitOfWork.Users.GetByIdAsync(task.AssigneeId.Value) : null;

        return MapToDto(task, project, creator, assignee);
    }

    public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto dto, ulong userId)
    {
        // Verify project exists
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId);
        if (project == null)
        {
            return null;
        }

        // Verify assignee exists if provided
        if (dto.AssigneeId.HasValue)
        {
            var assignee = await _unitOfWork.Users.GetByIdAsync(dto.AssigneeId.Value);
            if (assignee == null)
            {
                return null;
            }
        }

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            Type = dto.Type,
            AssigneeId = dto.AssigneeId,
            DueDate = dto.DueDate,
            Tags = dto.Tags,
            ProjectId = dto.ProjectId,
            SprintId = dto.SprintId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.Tasks.AddAsync(task);
        if (!added || !await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(userId);
        var assigneeUser = task.AssigneeId.HasValue ? await _unitOfWork.Users.GetByIdAsync(task.AssigneeId.Value) : null;

        return MapToDto(task, project, creator, assigneeUser);
    }

    public async Task<TaskDto?> UpdateTaskAsync(ulong id, UpdateTaskDto dto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return null;
        }

        // Verify assignee exists if provided
        if (dto.AssigneeId.HasValue)
        {
            var assignee = await _unitOfWork.Users.GetByIdAsync(dto.AssigneeId.Value);
            if (assignee == null)
            {
                return null;
            }
        }

        if (dto.Title != null) task.Title = dto.Title;
        if (dto.Description != null) task.Description = dto.Description;
        if (dto.Status.HasValue) task.Status = dto.Status.Value;
        if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;
        if (dto.Type.HasValue) task.Type = dto.Type.Value;
        if (dto.AssigneeId.HasValue) task.AssigneeId = dto.AssigneeId;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
        if (dto.Tags != null) task.Tags = dto.Tags;
        if (dto.SprintId.HasValue) task.SprintId = dto.SprintId;
        task.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Tasks.Update(task);
        if (!await _unitOfWork.CompleteAsync())
        {
            return null;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);
        var creator = await _unitOfWork.Users.GetByIdAsync(task.CreatedBy);
        var assigneeUser = task.AssigneeId.HasValue ? await _unitOfWork.Users.GetByIdAsync(task.AssigneeId.Value) : null;

        return MapToDto(task, project, creator, assigneeUser);
    }

    public async Task<bool> UpdateTaskStatusAsync(ulong id, UpdateTaskStatusDto dto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return false;
        }

        task.Status = dto.Status;
        task.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Tasks.Update(task);
        return await _unitOfWork.CompleteAsync();
    }

    public async Task<bool> UpdateTaskDueDateAsync(ulong id, UpdateTaskDueDateDto dto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return false;
        }

        task.DueDate = dto.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Tasks.Update(task);
        return await _unitOfWork.CompleteAsync();
    }

    public async Task<bool> DeleteTaskAsync(ulong id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return false;
        }

        _unitOfWork.Tasks.Remove(task);
        return await _unitOfWork.CompleteAsync();
    }

    private TaskDto MapToDto(TaskItem task, Project? project, User? creator, User? assignee)
    {
        // Generate project key from project name (first 3-4 letters uppercase)
        string projectKey = project != null
            ? new string(project.Name.Where(char.IsLetter).Take(4).ToArray()).ToUpper()
            : "PROJ";

        // Generate task key as ProjectKey-TaskId
        string taskKey = $"{projectKey}-{task.Id}";

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            StatusName = task.Status.ToString(),
            Priority = task.Priority,
            PriorityName = task.Priority.ToString(),
            Type = task.Type,
            TypeName = task.Type.ToString(),
            AssigneeId = task.AssigneeId,
            AssigneeName = assignee != null ? $"{assignee.FirstName} {assignee.LastName}" : null,
            DueDate = task.DueDate,
            Tags = task.Tags,
            CommentCount = task.Comments?.Count ?? 0,
            ProjectId = task.ProjectId,
            ProjectName = project?.Name ?? "Unknown",
            ProjectKey = projectKey,
            TaskKey = taskKey,
            SprintId = task.SprintId,
            SprintName = task.Sprint?.Name,
            CreatedBy = task.CreatedBy,
            CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}

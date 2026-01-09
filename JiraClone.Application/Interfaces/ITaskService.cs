using JiraClone.Application.DTOs.Tasks;
using JiraClone.Domain.Enums;

namespace JiraClone.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetAllTasksAsync(ulong? projectId = null, Domain.Enums.TaskStatus? status = null, ulong? assigneeId = null, Domain.Enums.TaskPriority? priority = null, ulong? sprintId = null);
    Task<TaskDto?> GetTaskByIdAsync(ulong id);
    Task<TaskDto?> CreateTaskAsync(CreateTaskDto dto, ulong userId);
    Task<TaskDto?> UpdateTaskAsync(ulong id, UpdateTaskDto dto);
    Task<bool> UpdateTaskStatusAsync(ulong id, UpdateTaskStatusDto dto);
    Task<bool> UpdateTaskDueDateAsync(ulong id, UpdateTaskDueDateDto dto);
    Task<bool> DeleteTaskAsync(ulong id);
}

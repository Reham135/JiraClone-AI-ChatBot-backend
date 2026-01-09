using System.ComponentModel.DataAnnotations;
using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Tasks;

public class UpdateTaskStatusDto
{
    [Required]
    public Domain.Enums.TaskStatus Status { get; set; }
}

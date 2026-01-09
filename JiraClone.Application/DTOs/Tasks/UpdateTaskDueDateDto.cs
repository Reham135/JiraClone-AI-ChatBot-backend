using System.ComponentModel.DataAnnotations;

namespace JiraClone.Application.DTOs.Tasks;

public class UpdateTaskDueDateDto
{
    [Required]
    public DateTime DueDate { get; set; }
}

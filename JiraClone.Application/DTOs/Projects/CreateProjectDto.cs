using System.ComponentModel.DataAnnotations;

namespace JiraClone.Application.DTOs.Projects;

public class CreateProjectDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}

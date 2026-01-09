using System.ComponentModel.DataAnnotations;

namespace JiraClone.Application.DTOs.Comments;

public class CreateCommentDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public ulong TaskId { get; set; }
}

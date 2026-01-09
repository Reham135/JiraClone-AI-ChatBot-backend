namespace JiraClone.Application.DTOs.Comments;

public class CommentDto
{
    public ulong Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ulong TaskId { get; set; }
    public ulong CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

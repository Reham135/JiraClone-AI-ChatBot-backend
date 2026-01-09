using JiraClone.Application.DTOs.Comments;

namespace JiraClone.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsByTaskIdAsync(ulong taskId);
    Task<CommentDto?> GetCommentByIdAsync(ulong id);
    Task<CommentDto?> CreateCommentAsync(CreateCommentDto dto, ulong userId);
    Task<CommentDto?> UpdateCommentAsync(ulong id, UpdateCommentDto dto, ulong userId);
    Task<bool> DeleteCommentAsync(ulong id, ulong userId);
}

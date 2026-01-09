using JiraClone.Application.DTOs.Chat;
using Microsoft.AspNetCore.Http;

namespace JiraClone.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request, ulong userId);
        Task<ChatResponseDto> HandleFileUploadAsync(IFormFile file, ulong userId);
        void ClearConversation(ulong userId);
    }
}

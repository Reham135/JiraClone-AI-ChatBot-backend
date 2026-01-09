using System.ComponentModel.DataAnnotations;

namespace JiraClone.Application.DTOs.Chat
{
    public class ChatRequestDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public ulong? ProjectId { get; set; }

        public ulong? SprintId { get; set; }

        public ulong? AssigneeId { get; set; }
    }
}

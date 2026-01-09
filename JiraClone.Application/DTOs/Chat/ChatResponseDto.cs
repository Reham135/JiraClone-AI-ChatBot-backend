namespace JiraClone.Application.DTOs.Chat
{
    public class ChatResponseDto
    {
        public string Message { get; set; } = string.Empty;

        public TaskCreatedDto? TaskCreated { get; set; }

        public List<ChatChoice>? Choices { get; set; }

        public bool ExpectsInput { get; set; } = false;

        public string? InputType { get; set; } // "text", "date", "file"
    }

    public class ChatChoice
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class TaskCreatedDto
    {
        public ulong Id { get; set; }

        public string TaskKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}

using JiraClone.Domain.Enums;

namespace JiraClone.Application.DTOs.Chat
{
    public class ConversationState
    {
        public string ConversationId { get; set; } = Guid.NewGuid().ToString();
        public ulong UserId { get; set; }
        public ConversationStep CurrentStep { get; set; } = ConversationStep.AskType;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        // Task data being collected
        public TaskType? Type { get; set; }
        public ulong? ProjectId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TaskPriority? Priority { get; set; }
        public string? AssigneeName { get; set; }
        public ulong? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public ulong? SprintId { get; set; }
        public bool? WantsAttachment { get; set; }
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }

    public enum ConversationStep
    {
        AskType,              // Bug, Task, or Story?
        AskProject,           // Which project?
        AskDescription,       // Describe the task
        AskPriority,          // Priority level?
        AskAssignee,          // Who to assign?
        AskDueDate,           // Due date?
        AskSprintOrBacklog,   // Sprint or backlog?
        AskAttachment,        // Want to upload?
        UploadAttachment,     // Uploading files
        Complete              // Ready to create
    }
}

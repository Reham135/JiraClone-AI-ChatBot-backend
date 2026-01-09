using Microsoft.AspNetCore.Identity;

namespace JiraClone.Domain.Entities;

public class User : IdentityUser<ulong>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

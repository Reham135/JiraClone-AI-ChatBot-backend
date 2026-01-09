namespace JiraClone.Domain.Entities;

public class Attachment
{
    public ulong Id { get; set; }
    public ulong TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // image/jpeg, video/mp4, etc.
    public long FileSize { get; set; } // in bytes
    public ulong UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }

    // Navigation properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User UploadedByUser { get; set; } = null!;
}

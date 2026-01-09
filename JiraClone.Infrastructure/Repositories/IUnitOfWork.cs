using JiraClone.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace JiraClone.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<Project> Projects { get; }
    IRepository<TaskItem> Tasks { get; }
    IRepository<User> Users { get; }
    IRepository<Sprint> Sprints { get; }
    IRepository<Comment> Comments { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<Attachment> Attachments { get; }

    Task<bool> CompleteAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

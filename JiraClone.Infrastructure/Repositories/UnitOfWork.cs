using JiraClone.Domain.Entities;
using JiraClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace JiraClone.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly JiraCloneDbContext _context;
    private IDbContextTransaction? _transaction;

    public IRepository<Project> Projects { get; private set; }
    public IRepository<TaskItem> Tasks { get; private set; }
    public IRepository<User> Users { get; private set; }
    public IRepository<Sprint> Sprints { get; private set; }
    public IRepository<Comment> Comments { get; private set; }
    public IRepository<Notification> Notifications { get; private set; }
    public IRepository<Attachment> Attachments { get; private set; }

    public UnitOfWork(JiraCloneDbContext context)
    {
        _context = context;
        Projects = new Repository<Project>(_context);
        Tasks = new Repository<TaskItem>(_context);
        Users = new Repository<User>(_context);
        Sprints = new Repository<Sprint>(_context);
        Comments = new Repository<Comment>(_context);
        Notifications = new Repository<Notification>(_context);
        Attachments = new Repository<Attachment>(_context);
    }

    public async Task<bool> CompleteAsync()
    {
        try
        {
            return await _context.SaveChangesAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        return _transaction;
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JiraClone.Infrastructure.Data;

public class JiraCloneDbContextFactory : IDesignTimeDbContextFactory<JiraCloneDbContext>
{
    public JiraCloneDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JiraCloneDbContext>();

        // Use a dummy connection string for design-time (no actual connection needed for migrations)
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=JiraCloneDb;Trusted_Connection=true;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(
            connectionString,
            options => options.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            )
        );

        return new JiraCloneDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace PollyDemos.EntityFramework;

/// <summary>
/// Represents a database context for TodoItems.
/// </summary>
public class TodoDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the TodoItems DbSet.
    /// </summary>
    public DbSet<TodoItem> TodoItems { get; set; }

    /// <summary>
    /// Configures the database context options.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Create a logger factory and add a console logger.
        var loggerFactory = LoggerFactory.Create(factory => factory.AddConsole());

        optionsBuilder.UseLoggerFactory(loggerFactory);
        // Replace the default execution strategy factory with our own Polly-based implementation.
        optionsBuilder.ReplaceService<IExecutionStrategyFactory, PollyExecutionStrategyFactory>();
        optionsBuilder.UseInMemoryDatabase("data");

        base.OnConfiguring(optionsBuilder);
    }
}

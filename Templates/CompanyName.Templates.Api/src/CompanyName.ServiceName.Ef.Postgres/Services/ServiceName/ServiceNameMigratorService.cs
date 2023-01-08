using CompanyName.ServiceName.Ef.Data.ServiceName;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyName.ServiceName.Ef.Postgres.Services.ServiceName;

public class ServiceNameMigratorService
{
  private readonly IServiceNameDbContext _dbContext;
  private readonly ILogger<ServiceNameMigratorService> _logger;

  private readonly string _dbContextName;

  public ServiceNameMigratorService(
    IServiceNameDbContext dbContext,
    ILogger<ServiceNameMigratorService> logger
  )
  {
    _dbContext = dbContext;
    _logger = logger;

    _dbContextName = nameof(IServiceNameDbContext);
  }

  private Task SeedAsync(CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }

  private async Task TransactionSeedAsync(CancellationToken cancellationToken = default)
  {
    var context = (DbContext)_dbContext;
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
      await SeedAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception)
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }

  public async Task SeedAsync(int timeout = 300, CancellationToken cancellationToken = default)
  {
    _logger.LogWarning("Database \"{DbContextName}\" seeding started", _dbContextName);
    var context = (DbContext)_dbContext;
    var originalTimeout = context.Database.GetCommandTimeout();
    try
    {
      context.Database.SetCommandTimeout(timeout);
      await TransactionSeedAsync(cancellationToken);
      context.Database.SetCommandTimeout(originalTimeout);
      _logger.LogWarning("Database \"{DbContextName}\" seeding completed", _dbContextName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Seed exception in \"{DbContextName}\". Error: {ExceptionMessage}", _dbContextName,
        ex.Message);
      context.Database.SetCommandTimeout(originalTimeout);
      throw;
    }
  }

  public async Task MigrateAsync(int timeout = 60, CancellationToken cancellationToken = default)
  {
    _logger.LogWarning("Database \"{DbContextName}\" migration started", _dbContextName);
    var context = (DbContext)_dbContext;
    var originalTimeout = context.Database.GetCommandTimeout();
    try
    {
      var migrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
      if (migrations.Count > 0)
      {
        context.Database.SetCommandTimeout(timeout);
        await context.Database.MigrateAsync(cancellationToken);
        context.Database.SetCommandTimeout(originalTimeout);
        foreach (var migration in migrations)
        {
          _logger.LogWarning("Database \"{DbContextName}\" migration \"{Migration}\" completed",
            _dbContextName, migration);
        }

        _logger.LogWarning("All database \"{DbContextName}\" migrations completed", _dbContextName);
      }
      else
      {
        _logger.LogWarning("No new migrations in database \"{DbContextName}\"", _dbContextName);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Migration exception in \"{DbContextName}\". Error: {ExceptionMessage}", _dbContextName,
        ex.Message);
      context.Database.SetCommandTimeout(originalTimeout);
      throw;
    }
  }
}

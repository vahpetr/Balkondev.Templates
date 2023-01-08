using System.Text.RegularExpressions;

using CompanyName.ServiceName.Ef.Data.ServiceName;
using CompanyName.ServiceName.Ef.Postgres.Data.ServiceName;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyName.ServiceName.Ef.Postgres.Extensions;

public static partial class EntityFrameworkServiceCollectionExtensions
{
  // ReSharper disable once UnusedMethodReturnValue.Global
  public static IServiceCollection AddServiceNameDbContextPool(
    this IServiceCollection serviceCollection,
    string? connectionString,
    bool isDevelopment = false,
    Action<DbContextOptionsBuilder>? optionsAction = null,
    int poolSize = 1024
  )
  {
    if (connectionString == null)
    {
      throw new ArgumentNullException(nameof(connectionString));
    }

    return serviceCollection.AddDbContextPool<IServiceNameDbContext, ServiceNameDbContext>((_, options) =>
      {
        optionsAction?.Invoke(options);

        SetupOptionsBuilder(options, connectionString, isDevelopment);
      },
      poolSize
    );
  }


  public static void SetupOptionsBuilder(DbContextOptionsBuilder options, string? connectionString, bool isDevelopment)
  {
    if (connectionString == null)
    {
      throw new ArgumentNullException(nameof(connectionString));
    }

    options.EnableThreadSafetyChecks(false);

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);

    options.UseNpgsql(ParseConnectionString(connectionString), npgsqlOptions =>
      {
        npgsqlOptions.CommandTimeout(1)
          .MinBatchSize(1)
          .MinBatchSize(1000)
          .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
      })
      .UseSnakeCaseNamingConvention();

    if (isDevelopment)
    {
      options
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .EnableThreadSafetyChecks(true);
    }
  }

  public static string ParseConnectionString(string connectionString)
  {
    var regexp = GetConnectionStringRegex();

    // example "postgres://user:testing-password@staging-postgresql:5432/staging"
    var matches = regexp.Matches(connectionString);

    if (matches.Count != 6)
    {
      return connectionString;
    }

    var username = matches[1];
    var password = matches[2];
    var host = matches[3];
    var port = matches[4];
    var database = matches[5];

    // example "Host=localhost;Port=5432;Username=postgres;Password=pass;Database=postgres"
    return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
  }

  [GeneratedRegex("[^:/@]+")]
  private static partial Regex GetConnectionStringRegex();
}

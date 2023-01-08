using CompanyName.ServiceName.Ef.Postgres.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CompanyName.ServiceName.Ef.Postgres.Data.ServiceName;

public class ServiceNameContextFactory : IDesignTimeDbContextFactory<ServiceNameDbContext>
{
  public ServiceNameDbContext CreateDbContext(string[] args)
  {
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    var pwd = Directory.GetCurrentDirectory();
    var cfg = new ConfigurationBuilder()
      .AddJsonFile($"{pwd}/../CompanyName.ServiceName.Api/appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile($"{pwd}/../CompanyName.ServiceName.Api/appsettings.{env}.json", optional: true, reloadOnChange: false)
      .AddEnvironmentVariables()
      .AddCommandLine(args)
      .Build();

    var optionsBuilder = new DbContextOptionsBuilder<ServiceNameDbContext>();
    var connectionString =
      Environment.GetEnvironmentVariable("DATABASE_URL") ?? cfg.GetConnectionString("ServiceName");

    EntityFrameworkServiceCollectionExtensions.SetupOptionsBuilder(optionsBuilder, connectionString, env == "Development");

    return new ServiceNameDbContext(optionsBuilder.Options);
  }
}

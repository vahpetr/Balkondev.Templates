using CompanyName.ServiceName.V1;

using Microsoft.EntityFrameworkCore;

namespace CompanyName.ServiceName.Ef.Data.ServiceName;

public interface IServiceNameDbContext
{
  public DbSet<EntityName> EntityNames { get; }
}

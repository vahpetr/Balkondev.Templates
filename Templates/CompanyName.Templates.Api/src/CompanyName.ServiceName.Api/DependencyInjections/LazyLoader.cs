namespace CompanyName.ServiceName.Api.DependencyInjections;

public class LazyLoader<T> : Lazy<T> where T : notnull
{
  public LazyLoader(IServiceProvider sp) : base(() => sp.GetRequiredService<T>()!)
  {
  }
}

using System.Security.Claims;

using CompanyName.ServiceName.Shared;

namespace CompanyName.ServiceName.Api.Context;

public class ServiceNameContext : IServiceNameContext
{
  public DateTimeOffset Now { get; init; }
  public ClaimsPrincipal? Principal { get; init; }
}

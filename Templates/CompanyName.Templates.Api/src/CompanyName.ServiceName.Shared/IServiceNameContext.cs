using System.Security.Claims;

namespace CompanyName.ServiceName.Shared;

public interface IServiceNameContext
{
  DateTimeOffset Now { get; init; }
  ClaimsPrincipal? Principal { get; init; }
}

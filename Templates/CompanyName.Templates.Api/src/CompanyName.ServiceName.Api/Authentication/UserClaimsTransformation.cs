using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

namespace CompanyName.ServiceName.Api.Authentication;

public class UserClaimsTransformation : IClaimsTransformation
{
  public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    return Task.FromResult(principal);
  }
}

using System.Net;

namespace CompanyName.ServiceName.ConsoleClient;

public class Http3SupportedHandler : DelegatingHandler
{
  public Http3SupportedHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
  {
    request.Version = HttpVersion.Version20;
    request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

    return base.SendAsync(request, cancellationToken);
  }
}

using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;

using Microsoft.Extensions.Logging;

namespace CompanyName.ServiceName.ConsoleClient;

public static class GrpcChannelBuilder
{
  public static GrpcChannel Build(string apiUrl, bool isSecure = false, string? accessToken = null)
  {
    // credentials
    var credentials = isSecure ? new SslCredentials() : ChannelCredentials.Insecure;
    var channelCredentials = accessToken != null
      ? ChannelCredentials.Create(credentials, CallCredentials.FromInterceptor((_, metadata) =>
      {
        metadata.Add("Authorization", $"Bearer {accessToken}");
        return Task.CompletedTask;
      }))
      : credentials;

    // http handler
    var httpHandler = new SocketsHttpHandler
    {
      PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
      KeepAlivePingDelay = TimeSpan.FromSeconds(60),
      KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
      EnableMultipleHttp2Connections = true,
      // ...configure other handler settings
    };

    // logger
    var loggerFactory = LoggerFactory.Create(logging =>
    {
      logging.AddConsole();
    });

    // load balancer
    // https://docs.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-6.0
    var loadBalancingConfig = new RoundRobinConfig();

    // retry policy
    // https://docs.microsoft.com/ru-ru/aspnet/core/grpc/retries?view=aspnetcore-6.0#configure-a-grpc-retry-policy
    var methodConfig = new MethodConfig
    {
      Names = { MethodName.Default },
      RetryPolicy = new RetryPolicy
      {
        MaxAttempts = 3,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(5),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = { StatusCode.Unavailable }
      }
    };

    // DI
    // var services = new ServiceCollection();

    // create channel
    var channel = GrpcChannel.ForAddress(
      apiUrl,
      new GrpcChannelOptions
      {
        LoggerFactory = loggerFactory,
        HttpHandler = new Http3SupportedHandler(httpHandler),
        ServiceConfig = new ServiceConfig
        {
          MethodConfigs = { methodConfig },
          LoadBalancingConfigs = { loadBalancingConfig }
        },
        // ServiceProvider = services.BuildServiceProvider(),
        Credentials = channelCredentials
      }
    );

    return channel;
  }
}

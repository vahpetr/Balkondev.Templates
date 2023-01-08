using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using CompanyName.ServiceName.Api.Authentication;
using CompanyName.ServiceName.Api.Context;
using CompanyName.ServiceName.Api.DependencyInjections;
using CompanyName.ServiceName.Api.JsonConverters;
using CompanyName.ServiceName.Ef.Postgres.Extensions;
using CompanyName.ServiceName.Ef.Postgres.Services.ServiceName;
using CompanyName.ServiceName.Shared;

using Google.Protobuf.WellKnownTypes;

using Grpc.Net.Compression;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;


if (args.Length == 0) throw new ArgumentException("Please setup run arguments!");

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();

builder.WebHost.ConfigureKestrel(o =>
{
  o.AddServerHeader = false;
  o.Limits.Http2.InitialConnectionWindowSize = 64 * 1024; // 64kb // default 128kb
  o.Limits.Http2.InitialStreamWindowSize = 64 * 1024; // 64kb // default 96kb
});

builder.Services.AddCors(o =>
{
  var origins = (builder.Configuration["Origins"] ?? string.Empty!).Split(',');
  o.AddDefaultPolicy(policy =>
    {
      policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowedToAllowWildcardSubdomains();

      if (origins[0] == "*")
      {
        policy.SetIsOriginAllowed(_ => true);
      }
      else
      {
        policy.WithOrigins(origins);
      }
    }
  );
});

// https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-7.0
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization(o =>
{
  o.AddPolicy("ServiceNameFullAccessPolicy",
    b =>
    {
      var scopes = new[] { "servicenamelower:full" };
      b.RequireAssertion(ctx => ctx.User.HasClaim(
        claim => claim.Type == "scope" && claim.Value.Split(' ').Any(v => scopes.Contains(v))
      ));
    });
  o.AddPolicy("ServiceNameReadAccessPolicy",
    b =>
    {
      var scopes = new[] { "servicenamelower:read", "servicenamelower:full" };
      b.RequireAssertion(ctx => ctx.User.HasClaim(
        claim => claim.Type == "scope" && claim.Value.Split(' ').Any(v => scopes.Contains(v))
      ));
    });
});

// https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
  o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  o.KnownNetworks.Clear();
  o.KnownProxies.Clear();
});

builder.Services.AddTransient(typeof(Lazy<>), typeof(LazyLoader<>));

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                       builder.Configuration.GetConnectionString("ServiceName");
builder.Services.AddServiceNameDbContextPool(connectionString, builder.Environment.IsDevelopment());

builder.Services.AddScoped<ServiceNameMigratorService>();

builder.Services.AddScoped<IClaimsTransformation, UserClaimsTransformation>();
builder.Services.AddScoped<IServiceNameContext, ServiceNameContext>(p => new ServiceNameContext
{
  Now = DateTimeOffset.UtcNow, Principal = p.GetService<ClaimsPrincipal>()
});

if (args.Contains("--grpc"))
{
  builder.Services.AddGrpc(o =>
  {
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    o.MaxReceiveMessageSize = 64 * 1024; // 64kb // default 4mb
    o.MaxSendMessageSize = 64 * 1024; // 64kb // 5mb
    o.CompressionProviders.Add(new DeflateCompressionProvider(CompressionLevel.Fastest));
  });
}

if (args.Contains("--grpc-reflection"))
{
  builder.Services.AddGrpcReflection();
}

if (args.Contains("--http"))
{
  // https://docs.microsoft.com/ru-ru/aspnet/core/performance/response-compression
  builder.Services.AddResponseCompression(o =>
  {
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
  });
  builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
  {
    o.Level = CompressionLevel.Fastest;
  });
  builder.Services.AddResponseCaching(o =>
  {
    o.MaximumBodySize = 256 * 1024; // 256kb
    o.UseCaseSensitivePaths = false;
  });
  builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(p => Configure(p.SerializerOptions));
}

if (args.Contains("--http-swagger"))
{
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddGrpcSwagger();
  builder.Services.AddSwaggerGen(c =>
  {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServiceName", Version = "v1" });
    var apiXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    c.IncludeXmlComments(apiXmlDoc);
    var grpcXmlDoc = Path.Combine(AppContext.BaseDirectory, "CompanyName.ServiceName.Server.xml");
    c.IncludeXmlComments(grpcXmlDoc);
    c.IncludeGrpcXmlComments(grpcXmlDoc, includeControllerXmlComments: true);
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
    c.DescribeAllParametersInCamelCase();
    c.UseInlineDefinitionsForEnums();
    c.MapType<Timestamp>(() => new OpenApiSchema { Type = "string", Format = "date-time" });
  });
  builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(p => Configure(p.JsonSerializerOptions));
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (args.Contains("--migrate"))
{
  using var scope = app.Services.CreateScope();
  await scope.ServiceProvider.GetRequiredService<ServiceNameMigratorService>().MigrateAsync();
}

if (args.Contains("--seed"))
{
  using var scope = app.Services.CreateScope();
  await scope.ServiceProvider.GetRequiredService<ServiceNameMigratorService>().SeedAsync();
}

if (args.Contains("--grpc"))
{
  app.MapGrpcService<ServiceName>();
}

if (args.Contains("--grpc-reflection"))
{
  app.MapGrpcReflectionService();
}

if (args.Contains("--http"))
{
  app.UseForwardedHeaders();
  app.UseCors();
  app.UseResponseCompression();

  app.MapGet("/", () => "ServiceName");
  app.MapGet("/health", () => Task.CompletedTask);
  app.MapGet("/internal/health", () => Task.CompletedTask);
}

if (args.Contains("--http-swagger"))
{
  app.UseSwagger(c =>
  {
    c.RouteTemplate = "/internal/swagger/{documentname}/swagger.json";
  });
  app.UseSwaggerUI(c =>
  {
    c.SwaggerEndpoint("/internal/swagger/v1/swagger.json", "ServiceName v1");
    c.RoutePrefix = "internal/swagger";
  });
}

await app.RunAsync();

void Configure(JsonSerializerOptions o)
{
  var policy = JsonNamingPolicy.CamelCase;
  o.PropertyNamingPolicy = policy;
  o.DictionaryKeyPolicy = policy;
  o.Encoder = JavaScriptEncoder.Default;
  o.ReferenceHandler = ReferenceHandler.IgnoreCycles;
  o.ReadCommentHandling = JsonCommentHandling.Disallow;
  o.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
  o.WriteIndented = false;
  o.AllowTrailingCommas = false;
  o.IgnoreReadOnlyProperties = false;
  o.PropertyNameCaseInsensitive = true;
  o.DefaultBufferSize = 32 * 1024; // 32kb
  o.MaxDepth = 8;
  o.Converters.Add(new TimestampJsonConverter());
}

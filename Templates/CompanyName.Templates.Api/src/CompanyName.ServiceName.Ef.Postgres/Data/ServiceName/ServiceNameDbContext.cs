using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

using CompanyName.ServiceName.Ef.Data.ServiceName;
using CompanyName.ServiceName.Ef.ValueComparers;
using CompanyName.ServiceName.Ef.ValueConverters;
using CompanyName.ServiceName.V1;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Microsoft.EntityFrameworkCore;

namespace CompanyName.ServiceName.Ef.Postgres.Data.ServiceName;

public partial class ServiceNameDbContext : DbContext, IServiceNameDbContext
{
  public ServiceNameDbContext(DbContextOptions<ServiceNameDbContext> options) : base(options)
  {
  }

  public DbSet<EntityName> EntityNames => Set<EntityName>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.HasPostgresExtension("pg_trgm");
    builder.Entity<EntityName>(b =>
    {
      b.HasIndex(p => p.Id).IsDescending(true);
      b.Property(p => p.Title).HasMaxLength(256).IsRequired();
      b.Property(p => p.Description).HasMaxLength(4096);
      b.HasIndex(b => new { b.Title, b.Description, b.Content })
        .HasMethod("GIN").HasOperators("gin_trgm_ops")
        .IsTsVectorExpressionIndex("english");
      b.Property(p => p.CreatedAt).IsRequired();
      b.HasIndex(p => new { p.CreatedAt, p.Id }).IsDescending(true, true);
      b.Property(p => p.ChangedAt).IsRequired();
      b.HasIndex(p => new { p.ChangedAt, p.Id }).IsDescending(true, true);
    });
    builder.HasDefaultSchema("servicenamelower");
    AddComments(builder);
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder builder)
  {
    builder.Properties<ByteString>()
      .HaveMaxLength(256);
    builder
      .Properties<ByteString>()
      .HaveConversion<ByteStringConverter, ByteStringValueComparer>();
    builder
      .Properties<Timestamp>()
      .HaveConversion<TimestampConverter>();
  }

  private static void AddComments(ModelBuilder builder)
  {
    var tagsRegex = GetTagsRegex();
    var whitespaceRegex = GetWhitespaceRegex();
    var normalizerRegex = GetNormalizerRegex();

    var comments = new Dictionary<string, string>();

    using var xr = XmlReader.Create(Path.Combine(AppContext.BaseDirectory, "CompanyName.ServiceName.Server.xml"));
    while (xr.Read())
    {
      {
        var xmlName = xr["name"];
        if (xmlName == null)
        {
          continue;
        }

        var text = xr.ReadInnerXml();
        comments[xmlName] = whitespaceRegex.Replace(tagsRegex.Replace(text, string.Empty), " ").Trim();
      }
    }

    string CreateKey(string fullName, string? propertyName)
    {
      var key = normalizerRegex.Replace(fullName, string.Empty).Replace('+', '.');
      return propertyName != null ? $"{key}.{propertyName}" : key;
    }

    string GetTypeKey(System.Type type) => $"T:{CreateKey(type.FullName!, null)}";

    string GetPropertyKey(MemberInfo propertyInfo) =>
      $"P:{CreateKey(propertyInfo!.DeclaringType!.FullName!, propertyInfo.Name)}";

    foreach (var type in builder.Model.GetEntityTypes())
    {
      var typeKey = GetTypeKey(type.ClrType);
      if (comments.TryGetValue(typeKey, out string? typeComment))
      {
        type.SetComment(typeComment);
      }

      foreach (var property in type.GetProperties())
      {
        var propertyKey = GetPropertyKey(property.PropertyInfo!);
        if (comments.TryGetValue(propertyKey, out string? propertyComment))
        {
          property.SetComment(propertyComment);
        }
      }
    }
  }

  [GeneratedRegex("<.*?>")]
  private static partial Regex GetTagsRegex();

  [GeneratedRegex("\\s+")]
  private static partial Regex GetWhitespaceRegex();

  [GeneratedRegex("\\[.*\\]")]
  private static partial Regex GetNormalizerRegex();
}

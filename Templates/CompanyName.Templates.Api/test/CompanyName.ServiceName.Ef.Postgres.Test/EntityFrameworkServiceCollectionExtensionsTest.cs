using CompanyName.ServiceName.Ef.Postgres.Extensions;

namespace CompanyName.ServiceName.Ef.Postgres.Test;

public class EntityFrameworkServiceCollectionExtensionsTest
{
  [Fact]
  public void ParseConnectionStringTest1()
  {
    var input = "postgres://user:testing-password@staging-postgresql:5432/staging";
    var output = EntityFrameworkServiceCollectionExtensions.ParseConnectionString(input);
    var desired = "Host=staging-postgresql;Port=5432;Username=user;Password=testing-password;Database=staging";
    Assert.Equal(output, desired);
  }

  [Fact]
  public void ParseConnectionStringTest2()
  {
    var input = "Host=localhost;Port=5432;Username=postgres;Password=pass;Database=postgres";
    var output = EntityFrameworkServiceCollectionExtensions.ParseConnectionString(input);
    var desired = "Host=localhost;Port=5432;Username=postgres;Password=pass;Database=postgres";
    Assert.Equal(output, desired);
  }
}

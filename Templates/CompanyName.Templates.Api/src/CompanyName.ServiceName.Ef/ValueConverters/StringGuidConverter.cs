using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CompanyName.ServiceName.Ef.ValueConverters;

public class StringGuidConverter : ValueConverter<string, Guid>
{
  public StringGuidConverter() : base(
    v => new Guid(v),
    v => v.ToString()
  )
  {
  }
}

using Google.Protobuf.WellKnownTypes;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CompanyName.ServiceName.Ef.ValueConverters;

public class TimestampConverter : ValueConverter<Timestamp, DateTimeOffset>
{
  public TimestampConverter() : base(
    v => v.ToDateTimeOffset(),
    v => Timestamp.FromDateTimeOffset(v)
  )
  {
  }
}

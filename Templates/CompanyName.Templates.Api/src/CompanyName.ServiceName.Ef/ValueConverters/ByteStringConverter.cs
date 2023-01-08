using Google.Protobuf;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CompanyName.ServiceName.Ef.ValueConverters;

public class ByteStringConverter : ValueConverter<ByteString, string>
{
  public ByteStringConverter() : base(
    v => v.ToBase64(),
    v => ByteString.FromBase64(v)
  )
  {
  }
}

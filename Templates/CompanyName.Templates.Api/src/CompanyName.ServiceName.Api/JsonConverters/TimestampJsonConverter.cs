using System.Text.Json;
using System.Text.Json.Serialization;

using Google.Protobuf.WellKnownTypes;

using Type = System.Type;

namespace CompanyName.ServiceName.Api.JsonConverters;

public class TimestampJsonConverter : JsonConverter<Timestamp>
{
  public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    return Timestamp.FromDateTimeOffset(reader.GetDateTimeOffset());
  }

  public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options)
  {
    writer.WriteStringValue(value.ToDateTimeOffset().ToString("O"));
  }
}

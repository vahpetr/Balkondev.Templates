using Google.Protobuf;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CompanyName.ServiceName.Ef.ValueComparers;

public class ByteStringValueComparer : ValueComparer<ByteString>
{
  public ByteStringValueComparer() : base(
    (c1, c2) => c1 == c2,
    c => c.GetHashCode(),
    c => ByteString.FromBase64(c.ToBase64())
  )
  {
  }
}

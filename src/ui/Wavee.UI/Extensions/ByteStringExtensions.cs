using System.Numerics;
using Google.Protobuf;
using Org.BouncyCastle.Utilities;

namespace Wavee.UI.Extensions;

public static class ByteStringExtensions
{
    public static BigInteger ToBigInteger(this ByteString input)
    {
        return new BigInteger(input.Span, true, true);
    }
}
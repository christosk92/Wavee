using System.Text;

namespace Wavee.Spfy.Utils;

internal static class BytesExtensions
{
    public static string ToBase16(this ReadOnlySpan<byte> x)
    {
        var hex = new StringBuilder(x.Length * 2);
        foreach (var b in x)
        {
            hex.Append($"{b:x2}");
        }

        return hex.ToString();
    }
}
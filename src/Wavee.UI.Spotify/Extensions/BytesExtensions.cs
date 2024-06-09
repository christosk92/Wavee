using System;
using System.Text;

namespace Wavee.UI.Spotify.Extensions;

public static class BytesExtensions
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
    
    public static byte[] ToBase16(this string x)
    {
        var bytes = new byte[x.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(x.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
        }

        return bytes;
    }
}
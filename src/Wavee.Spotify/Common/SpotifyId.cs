using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace Wavee.Spotify.Common;

/// <summary>
/// A struct representing an audio item.
/// </summary>
/// <param name="Id">
/// The base-62 encoded ID of the audio item.
/// </param>
/// <param name="Type">
///  The type of the audio item.
/// </param>
/// <param name="Service">
///  The service the audio item is from.
/// </param>
public readonly record struct SpotifyId(BigInteger Id, SpotifyItemType Type, bool IsLocal = false)
{
    private const string BASE62_CHARS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// Returns a copy of the `SpotifyId` as an array of `SpotifyId::SIZE` (16) bytes in
    /// big-endian order.
    public ReadOnlySpan<byte> ToRaw()
    {
        ReadOnlySpan<byte> id = Id.ToByteArray(true, true);
        var padding = (int)SIZE - id.Length;

        Span<byte> temp = new byte[SIZE];
        if (padding > 0)
            id.CopyTo(temp[padding..]);
        else
            id[..(id.Length + padding)].CopyTo(temp);
        return temp;
    }

    private const uint SIZE = 16;

    public string ToBase16()
    {
        var result = Id;
        var length = (int)Math.Ceiling(BigInteger.Log(result, 256));

        const int requiredLength = 16;
        if (length < requiredLength) length = requiredLength; // Ensure the minimum length


        Span<byte> bytes = stackalloc byte[length];
        // var bytes = new List<byte>();
        int index = 0;
        while (result > 0)
        {
            //bytes.Insert(0, (byte)(result & 0xff));
            bytes[index++] = (byte)(result & 0xff);
            result >>= 8;
        }

        bytes.Reverse();

        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public string ToBase62()
    {
        if (!IsLocal)
        {
            var value = Id;
            var sb = new StringBuilder();

            while (value > 0)
            {
                value = BigInteger.DivRem(value, BASE62_CHARS.Length, out BigInteger remainder);
                sb.Insert(0, BASE62_CHARS[(int)remainder]);
            }

            //expected length is 22
            while (sb.Length < 22)
            {
                sb.Insert(0, '0');
            }

            return sb.ToString();
        }

        Span<byte> bytes = Id.ToByteArray();
        ReadOnlySpan<char> asSpanChar = MemoryMarshal.Cast<byte, char>(bytes);
        var asString = new string(asSpanChar);
        return asString;
    }

    public static SpotifyId FromUri(ReadOnlySpan<char> uri)
    {
        if (!uri.StartsWith("spotify:local:"))
        {
            //[local,spotify]:[itemtype]:[base62]
            var firstIndex = uri.IndexOf(':');
            ReadOnlySpan<char> service = uri.Slice(0, firstIndex);
            var lastIndex = uri.LastIndexOf(':');
            if (firstIndex != lastIndex)
            {
                ReadOnlySpan<char> type = uri.Slice(service.Length + 1, lastIndex - service.Length - 1);
                ReadOnlySpan<char> base62 = uri.Slice(lastIndex + 1);

                return FromBase62(base62, GetTypeFrom(type));
                //return new AudioId( GetTypeFrom(type), GetServiceFrom(service));
            }

            return new SpotifyId(0, SpotifyItemType.Track);
        }
        else
        {
            // :local: -> 7 
            var firstIndex = uri.IndexOf(':') + 7;
            var everythingAfterLocal = uri.Slice(firstIndex);
            var asBytes = everythingAfterLocal.AsBytes();
            var bigInt = new BigInteger(asBytes);
            return new SpotifyId(bigInt, SpotifyItemType.Track, true);
        }
    }

    public static SpotifyId FromBase62(ReadOnlySpan<char> base62,
        SpotifyItemType itemType)
    {
        var result = new BigInteger();
        foreach (var t in base62)
        {
            int digit = BASE62_CHARS.IndexOf(t);
            if (digit == -1)
                return new SpotifyId();

            result = BigInteger.Multiply(result, BASE62_CHARS.Length) + digit;
        }

        return new SpotifyId(result, itemType);
    }

    public override string ToString()
    {
        return $"spotify:{GetTypeString(Type, IsLocal)}:{ToBase62()}";
    }

    private static SpotifyItemType GetTypeFrom(ReadOnlySpan<char> type)
    {
        return type switch
        {
            track => SpotifyItemType.Track,
            album => SpotifyItemType.Album,
            artist => SpotifyItemType.Artist,
            playlist => SpotifyItemType.Playlist,
            episode => SpotifyItemType.PodcastEpisode,
            collection => SpotifyItemType.UserCollection,
            prerelease => SpotifyItemType.Prerelease,
            _ => SpotifyItemType.Unknown
        };
    }

    private static string GetTypeString(SpotifyItemType type, bool local)
    {
        if (!local)
        {
            return type switch
            {
                SpotifyItemType.Track => track,
                SpotifyItemType.Album => album,
                SpotifyItemType.Artist => artist,
                SpotifyItemType.Playlist => playlist,
                SpotifyItemType.PodcastEpisode => episode,
                SpotifyItemType.UserCollection => collection,
                SpotifyItemType.Prerelease => prerelease,
                SpotifyItemType.Unknown => unknown,
                _ => unknown
            };
        }
        else
        {
            return "local";
        }
    }

    const string local = "local";
    const string spotify = "spotify";

    const string track = "track";
    const string album = "album";
    const string artist = "artist";
    const string playlist = "playlist";
    const string episode = "episode";
    const string collection = "collection";
    const string prerelease = "prerelease";
    const string unknown = "unknown";

    public static SpotifyId FromRaw(ReadOnlySpan<byte> id, SpotifyItemType type)
    {
        var result = new BigInteger();
        foreach (var t in id)
        {
            result = BigInteger.Multiply(result, 256) + t;
        }

        return new SpotifyId(result, type);
    }

    public static bool TryParse(ReadOnlySpan<char> uri, out SpotifyId o)
    {
        o = default;
        if (uri.StartsWith("spotify:"))
        {
            o = FromUri(uri);
            return true;
        }

        return false;
    }
}
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy;

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
public readonly record struct SpotifyId(BigInteger Id, AudioItemType Type, bool IsLocal = false)
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

        return ((ReadOnlySpan<byte>)bytes).ToBase16();
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

    public static SpotifyId UserCollection() => new SpotifyId(0, AudioItemType.UserCollection);

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

            return new SpotifyId(0, AudioItemType.Track);
        }
        else
        {
            // :local: -> 7 
            var firstIndex = uri.IndexOf(':') + 7;
            var everythingAfterLocal = uri.Slice(firstIndex);
            var asBytes = everythingAfterLocal.AsBytes();
            var bigInt = new BigInteger(asBytes);
            return new SpotifyId(bigInt, AudioItemType.Track, true);
        }
    }

    public static SpotifyId FromBase62(ReadOnlySpan<char> base62,
        AudioItemType itemType)
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

    private static AudioItemType GetTypeFrom(ReadOnlySpan<char> type)
    {
        return type switch
        {
            track => AudioItemType.Track,
            album => AudioItemType.Album,
            artist => AudioItemType.Artist,
            playlist => AudioItemType.Playlist,
            episode => AudioItemType.PodcastEpisode,
            collection => AudioItemType.UserCollection,
            prerelease => AudioItemType.Prerelease,
            _ => AudioItemType.Unknown
        };
    }

    private static string GetTypeString(AudioItemType type, bool local)
    {
        if (!local)
        {
            return type switch
            {
                AudioItemType.Track => track,
                AudioItemType.Album => album,
                AudioItemType.Artist => artist,
                AudioItemType.Playlist => playlist,
                AudioItemType.PodcastEpisode => episode,
                AudioItemType.UserCollection => collection,
                AudioItemType.Prerelease => prerelease,
                AudioItemType.Unknown => unknown,
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

    public static SpotifyId FromRaw(ReadOnlySpan<byte> id, AudioItemType type)
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
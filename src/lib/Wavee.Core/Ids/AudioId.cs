using System.Numerics;
using System.Text;

namespace Wavee.Core.Ids;

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
public readonly record struct AudioId(BigInteger Id, AudioItemType Type, ServiceType Service)
{
    private const string BASE62_CHARS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public string ToBase16()
    {
        // var base62 = ToBase62();
        // BigInteger result = new BigInteger();
        // for (int i = 0; i < base62.Length; i++)
        // {
        //     int digit = BASE62_CHARS.IndexOf(base62[i]);
        //     if (digit == -1)
        //         throw new ArgumentException($"Invalid character '{base62[i]}'");
        //
        //     result = BigInteger.Multiply(result, BASE62_CHARS.Length) + digit;
        // }

        var result = Id;
        var length = (int)Math.Ceiling(BigInteger.Log(result, 256));
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

    public static AudioId FromUri(ReadOnlySpan<char> uri)
    {
        //[local,spotify]:[itemtype]:[base62]
        var firstIndex = uri.IndexOf(':');
        ReadOnlySpan<char> service = uri.Slice(0, firstIndex);
        var lastIndex = uri.LastIndexOf(':');
        if (firstIndex != lastIndex)
        {
            ReadOnlySpan<char> type = uri.Slice(service.Length + 1, lastIndex - service.Length - 1);
            ReadOnlySpan<char> base62 = uri.Slice(lastIndex + 1);

            return FromBase62(base62, GetTypeFrom(type), GetServiceFrom(service));
            //return new AudioId( GetTypeFrom(type), GetServiceFrom(service));
        }
        return new AudioId(0, AudioItemType.Track, ServiceType.Local);
    }

    public static AudioId FromBase62(ReadOnlySpan<char> base62,
        AudioItemType itemType,
        ServiceType serviceType)
    {
        var result = new BigInteger();
        foreach (var t in base62)
        {
            int digit = BASE62_CHARS.IndexOf(t);
            if (digit == -1)
                return new AudioId();

            result = BigInteger.Multiply(result, BASE62_CHARS.Length) + digit;
        }

        return new AudioId(result, itemType, serviceType);
    }

    public override string ToString()
    {
        return $"{GetServiceString(Service)}:{GetTypeString(Type)}:{ToBase62()}";
    }


    private static ServiceType GetServiceFrom(ReadOnlySpan<char> service)
    {
        return service switch
        {
            spotify => ServiceType.Spotify,
            local => ServiceType.Local,
            _ => ServiceType.Local
        };
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
            _ => AudioItemType.Unknown
        };
    }

    private static string GetTypeString(AudioItemType type)
    {
        return type switch
        {
            AudioItemType.Track => track,
            AudioItemType.Album => album,
            AudioItemType.Artist => artist,
            AudioItemType.Playlist => playlist,
            AudioItemType.PodcastEpisode => episode,
            AudioItemType.Unknown => unknown,
            _ => unknown
        };
    }

    private static string GetServiceString(ServiceType service)
    {
        return service switch
        {
            ServiceType.Local => local,
            ServiceType.Spotify => spotify,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }

    const string local = "local";
    const string spotify = "spotify";

    const string track = "track";
    const string album = "album";
    const string artist = "artist";
    const string playlist = "playlist";
    const string episode = "episode";
    const string unknown = "unknown";

    public static AudioId FromRaw(ReadOnlySpan<byte> id, AudioItemType type, ServiceType serviceType)
    {
        var result = new BigInteger();
        foreach (var t in id)
        {
            result = BigInteger.Multiply(result, 256) + t;
        }
        
        return new AudioId(result, type, serviceType);
    }
}
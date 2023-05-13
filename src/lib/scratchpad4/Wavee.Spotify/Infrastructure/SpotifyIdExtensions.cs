using System.Buffers;
using System.Numerics;
using System.Text;
using Spotify.Metadata;
using Wavee.Core.Id;

namespace Wavee.Spotify.Infrastructure;

internal static class SpotifyIdExtensions
{
    public static ReadOnlySpan<byte> ToRaw(this AudioId id)
    {
        //spotifyids are base62 encoded
        var raw = Decode(id.Id);
        return raw;
    }

    public static Option<AudioId> ParseUri(this string uri)
    {
        ReadOnlySpan<string> split = uri.Split(":");
        //var isLocal = split[0] is "local";
        var type = split[1] switch
        {
            "album" => AudioItemType.Album,
            "artist" => AudioItemType.Artist,
            "track" => AudioItemType.Track,
            "playlist" => AudioItemType.Playlist,
            _ => AudioItemType.Unknown
        };

        return new AudioId(split[2], type, ISpotifyCore.SourceId);
    }

    public static AudioId ToId(this Track track)
    {
        var gid = track.Gid.Span;
        var id = Encode(gid);
        return new AudioId(id, AudioItemType.Track, ISpotifyCore.SourceId);
    }

    public static AudioId ToId(this Episode episode)
    {
        var gid = episode.Gid.Span;
        var id = Encode(gid);
        return new AudioId(id, AudioItemType.PodcastEpisode, ISpotifyCore.SourceId);
    }

    public static AudioId ToId(this ArtistWithRole artist)
    {
        var gid = artist.ArtistGid.Span;
        var id = Encode(gid);
        return new AudioId(id, AudioItemType.Artist, ISpotifyCore.SourceId);
    }

    public static AudioId ToId(this Album album)
    {
        var gid = album.Gid.Span;
        var id = Encode(gid);
        return new AudioId(id, AudioItemType.Album, ISpotifyCore.SourceId);
    }

    public static string ToBase16(this Image image)
    {
        var raw = image.FileId.Span;
        //convert to hex
        var hex = new StringBuilder(raw.Length * 2);
        foreach (var b in raw)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public static string ToBase16(this AudioId id)
    {
        var raw = id.ToRaw();
        //convert to hex
        var hex = new StringBuilder(raw.Length * 2);
        foreach (var b in raw)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    private const string BASE62_CHARS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static string Encode(ReadOnlySpan<byte> raw)
    {
        BigInteger value = new BigInteger(raw, true, true);
        if (value == 0) return BASE62_CHARS[0].ToString();

        StringBuilder sb = new StringBuilder();
        while (value > 0)
        {
            value = BigInteger.DivRem(value, BASE62_CHARS.Length, out BigInteger remainder);
            sb.Insert(0, BASE62_CHARS[(int)remainder]);
        }

        return sb.ToString();
    }

    private static ReadOnlySpan<byte> Decode(string value)
    {
        BigInteger result = new BigInteger();
        for (int i = 0; i < value.Length; i++)
        {
            int digit = BASE62_CHARS.IndexOf(value[i]);
            if (digit == -1)
                throw new ArgumentException($"Invalid character '{value[i]}'");

            result = BigInteger.Multiply(result, BASE62_CHARS.Length) + digit;
        }

        // Convert to bytes
        if (result == 0) return new byte[1] { 0 };

        var bytes = new List<byte>();
        while (result > 0)
        {
            bytes.Insert(0, (byte)(result & 0xff));
            result >>= 8;
        }

        return bytes.ToArray();
    }
}
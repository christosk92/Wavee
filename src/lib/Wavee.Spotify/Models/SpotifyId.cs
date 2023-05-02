using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Base62;
using Google.Protobuf;
using Wavee.Enums;
using Wavee.Spotify.Helpers.Base;
using Wavee.Spotify.Helpers.Extensions;

namespace Wavee.Spotify.Models;


/// <summary>
/// A type of SpotifyId which implements from <see cref="IAudioId"/>.
/// </summary>
public readonly struct SpotifyId : IEquatable<SpotifyId>, IComparable<SpotifyId>
{
    [JsonConstructor]
    public SpotifyId(string uri)
    {
        Uri = uri;
        var s =
            uri.SplitLines();
        if (s.MoveNext())
        {
            if (s.MoveNext())
            {
                Type = GetType(s.Current.Line, uri);
                if (s.MoveNext())
                {
                    Id = s.Current.Line.ToString();
                    return;
                }
            }
        }

        throw new NotFiniteNumberException();
    }

    /// <summary>
    /// The source of the spotify id.
    /// For local tracks found in Spotify, this will report as <see cref="ServiceType.Local"/>.
    /// </summary>
    public ServiceType Source => ServiceType.Spotify;

    /// <summary>
    /// The spotify uri of the track.
    /// <example>
    /// For a track, this will be something like spotify:track:a2131bada
    /// </example>
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// The type this ID belongs to.
    /// </summary>
    public AudioItemType Type { get; }

    /// <summary>
    /// The spotify ID of the uri.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// This will always be Spotify.
    /// </summary>
    public ServiceType Service => ServiceType.Spotify;
    public bool Equals(SpotifyId other)
    {
        return Uri == other.Uri;
    }

    public override bool Equals(object obj)
    {
        return obj is SpotifyId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (Uri != null ? Uri.GetHashCode() : 0);
    }

    private static AudioItemType GetType(ReadOnlySpan<char> r,
        string uri)
    {
        switch (r)
        {
            case var start_group when
                start_group.SequenceEqual("start-group".AsSpan()):
                return AudioItemType.Unknown;
            case var end_group when
                end_group.SequenceEqual("end-group".AsSpan()):
                return AudioItemType.Unknown;
            case var end_group when
                end_group.SequenceEqual("station".AsSpan()):
                return AudioItemType.Station;
            case var track when
                track.SequenceEqual("track".AsSpan()):
                return AudioItemType.Track;
            case var artist when
                artist.SequenceEqual("artist".AsSpan()):
                return AudioItemType.Artist;
            case var album when
                album.SequenceEqual("album".AsSpan()):
                return AudioItemType.Album;
            case var show when
                show.SequenceEqual("show".AsSpan()):
                return AudioItemType.Show;
            case var episode when
                episode.SequenceEqual("episode".AsSpan()):
                return AudioItemType.Episode;
            case var playlist when
                playlist.SequenceEqual("playlist".AsSpan()):
                return AudioItemType.Playlist;
            case var collection when
                collection.SequenceEqual("collection".AsSpan()):
                return AudioItemType.Collection;
            case var app when
                app.SequenceEqual("app".AsSpan()):
                return AudioItemType.Unknown;
            case var dailymixhub when
                dailymixhub.SequenceEqual("daily-mix-hub".AsSpan()):
                return AudioItemType.Unknown;
            case var user when
                user.SequenceEqual("daily-mix-hub".AsSpan()):
                {
                    var regexMatch = Regex.Match(uri, "spotify:user:(.*):playlist:(.{22})");
                    if (regexMatch.Success)
                    {
                        return AudioItemType.Playlist;
                    }

                    regexMatch = Regex.Match(uri, "spotify:user:(.*):collection");
                    return regexMatch.Success ? AudioItemType.Unknown : AudioItemType.User;
                }
            default:
                return AudioItemType.Unknown;
        }
    }

    public int Compare(SpotifyId x, SpotifyId y)
    {
        return string.Compare(x.Uri, y.Uri, StringComparison.Ordinal);
    }

    public int CompareTo(SpotifyId other)
    {
        return string.Compare(Uri, other.Uri, StringComparison.Ordinal);
    }

    public static SpotifyId FromHex(string hex, AudioItemType type)
    {
        var k = Base62Test.Encode(HexToBytes(hex));
        var j = $"spotify:{type.ToString().ToLower()}:" + Encoding.Default.GetString(k);
        return new SpotifyId(j);
    }

    public string ToHex()
    {
        ReadOnlySpan<byte> k = Id.FromBase62(true);
        return (BytesToHex(k, 0, k.Length, true, 0)).ToLowerInvariant();
    }

    private static readonly Base62Test Base62Test
        = Base62Test.CreateInstanceWithInvertedCharacterSet();

    public static SpotifyId FromGid(ByteString albumGid, AudioItemType album)
    {
        return SpotifyId.FromHex(BytesToHex(albumGid.Span), album);
    }
    public static SpotifyId FromRaw(ReadOnlySpan<byte> id, AudioItemType album)
    {
        return SpotifyId.FromHex(BytesToHex(id), album);
    }
    internal static byte[] HexToBytes(string str)
    {
        var len = str.Length;
        var data = new byte[len / 2];
        for (var i = 0; i < len; i += 2)
            data[i / 2] = (byte)((Convert.ToInt32(str[i].ToString(),
                16) << 4) + Convert.ToInt32(str[i + 1].ToString(), 16));
        return data;
    }

    internal static string BytesToHex(ReadOnlySpan<byte> bytes)
    {
        return BytesToHex(bytes, 0, bytes.Length, false, -1);
    }

    internal static string BytesToHex(ReadOnlySpan<byte> bytes, int offset, int length, bool trim, int minLength)
    {
        int newOffset = 0;
        bool trimming = trim;
        char[] hexChars = new char[length * 2];
        for (int j = offset; j < length; j++)
        {
            int v = bytes[j] & 0xFF;
            if (trimming)
            {
                if (v == 0)
                {
                    newOffset = j + 1;

                    if (minLength != -1 && length - newOffset == minLength)
                        trimming = false;

                    continue;
                }
                else
                {
                    trimming = false;
                }
            }

            hexChars[j * 2] = hexArray[(uint)v >> 4];
            hexChars[j * 2 + 1] = hexArray[v & 0x0F];
        }

        return new string(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
    }

    private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();

    /// Returns a copy of the `SpotifyId` as an array of `SpotifyId::SIZE` (16) bytes in
    /// big-endian order.
    public ReadOnlySpan<byte> ToRaw()
    {
        ReadOnlySpan<byte> id = Id.FromBase62(true);
        var padding = (int)SIZE - id.Length;

        Span<byte> temp = new byte[SIZE];
        if (padding > 0)
            id.CopyTo(temp[padding..]);
        else
            id[..(id.Length + padding)].CopyTo(temp);
        return temp;
    }
    private const uint SIZE = 16;
}
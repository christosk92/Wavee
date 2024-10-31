using System.Numerics;
using System.Text;
using Google.Protobuf;
using Wavee.Enums;
using Wavee.Exceptions;

namespace Wavee.Models.Common;

public struct SpotifyId : IEquatable<SpotifyId>
{
    private const int SIZE = 16;
    private const int SIZE_BASE16 = 32;
    private const int SIZE_BASE62 = 22;
    private const int UID_BYTE_LENGTH = 10; // 10 bytes for the UID
    private const int UID_HEX_LENGTH = 20; // 20 hex characters

    private static readonly char[] BASE62_DIGITS =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private static readonly string BASE62_DIGITS_STR =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";


    private static readonly char[] BASE16_DIGITS = "0123456789abcdef".ToCharArray();

    public BigInteger Id { get; }
    public SpotifyItemType ItemType { get; }

    public SpotifyId(BigInteger id, SpotifyItemType itemType)
    {
        Id = id;
        ItemType = itemType;
    }

    public bool IsPlayable() => ItemType == SpotifyItemType.Episode || ItemType == SpotifyItemType.Track;

    public static SpotifyId FromBase16(string src)
    {
        if (src.Length != SIZE_BASE16)
            throw new SpotifyIdException("Invalid ID");

        UInt128 dst = 0;
        foreach (char c in src)
        {
            UInt128 p = c switch
            {
                >= '0' and <= '9' => (UInt128)(c - '0'),
                >= 'a' and <= 'f' => (UInt128)(c - 'a' + 10),
                _ => throw new SpotifyIdException("Invalid ID")
            };

            dst <<= 4;
            dst += p;
        }

        return new SpotifyId(dst, SpotifyItemType.Unknown);
    }

    public static SpotifyId FromBase62(string src, SpotifyItemType type = SpotifyItemType.Unknown)
    {
        if (src.Length != SIZE_BASE62)
            throw new SpotifyIdException("Invalid ID");

        UInt128 dst = 0;
        foreach (char c in src)
        {
            UInt128 p = c switch
            {
                >= '0' and <= '9' => (UInt128)(c - '0'),
                >= 'a' and <= 'z' => (UInt128)(c - 'a' + 10),
                >= 'A' and <= 'Z' => (UInt128)(c - 'A' + 36),
                _ => throw new SpotifyIdException("Invalid ID")
            };

            dst = checked(dst * 62 + p);
        }

        return new SpotifyId(dst, type);
    }


    public static SpotifyId FromRaw(byte[] src)
    {
        if (src.Length != SIZE)
            throw new SpotifyIdException("Invalid ID");

        return new SpotifyId(new BigInteger(src.Reverse().ToArray()), SpotifyItemType.Unknown);
    }

    public static SpotifyId FromRaw(ReadOnlySpan<byte> src, SpotifyItemType type)
    {
        if (src.Length != SIZE)
            throw new SpotifyIdException("Invalid ID");

        return new SpotifyId(new BigInteger(src, true, true), type);
    }


    public static string ToBase16(ByteString bs)
    {
        var sb = new StringBuilder();
        foreach (var b in bs)
        {
            sb.Append(BASE16_DIGITS[b >> 4]);
            sb.Append(BASE16_DIGITS[b & 0xf]);
        }

        return sb.ToString();
    }

    public static Span<byte> ToBase16Bytes(BigInteger id)
    {
        var result = id;
        var length = (int)Math.Ceiling(BigInteger.Log(result, 256));

        const int requiredLength = 16;
        if (length < requiredLength) length = requiredLength; // Ensure the minimum length


        Span<byte> bytes = new byte[length];
        // var bytes = new List<byte>();
        int index = 0;
        while (result > 0)
        {
            //bytes.Insert(0, (byte)(result & 0xff));
            bytes[index++] = (byte)(result & 0xff);
            result >>= 8;
        }

        bytes.Reverse();
        return bytes;
    }

    public Span<byte> ToBase16Bytes()
    {
        var result = Id;
        var length = (int)Math.Ceiling(BigInteger.Log(result, 256));

        const int requiredLength = 16;
        if (length < requiredLength) length = requiredLength; // Ensure the minimum length


        Span<byte> bytes = new byte[length];
        // var bytes = new List<byte>();
        int index = 0;
        while (result > 0)
        {
            //bytes.Insert(0, (byte)(result & 0xff));
            bytes[index++] = (byte)(result & 0xff);
            result >>= 8;
        }

        bytes.Reverse();
        return bytes;
    }


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

        //return ((ReadOnlySpan<byte>)bytes).ToBase16();
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(BASE16_DIGITS[b >> 4]);
            sb.Append(BASE16_DIGITS[b & 0xf]);
        }

        return sb.ToString();
    }

    public string ToBase62()
    {
        if (ItemType is SpotifyItemType.FolderUnknown or SpotifyItemType.FolderStart or SpotifyItemType.FolderEnd or SpotifyItemType.Local)
        {
            var bytes = Id.ToByteArray();
            return Encoding.UTF8.GetString(bytes);
        }

        // Optimized code for BigInteger handling
        BigInteger n = Id;
        const int Base62Length = 22;
        Span<char> base62Chars = stackalloc char[Base62Length];
        int i = Base62Length;

        while (n > 0)
        {
            // Calculate the next digit in Base62 and decrement i to fill from the back
            base62Chars[--i] = BASE62_DIGITS[(int)(n % 62)];
            n /= 62;
        }

        // If the number is smaller than expected, we pad it with '0'
        // This ensures that the string always has the required length
        while (i > 0)
        {
            base62Chars[--i] = BASE62_DIGITS[0]; // Leading '0' as padding if needed
        }

        return new string(base62Chars);
    }

    public byte[] ToRaw()
    {
        byte[] bytes = Id.ToByteArray();
        if (bytes.Length < SIZE)
        {
            return bytes.Reverse().Concat(new byte[SIZE - bytes.Length]).ToArray();
        }

        return bytes.Take(SIZE).Reverse().ToArray();
    }

    public static SpotifyId FromUri(string src)
    {
        string[] parts = src.Split(':');

        if (parts.Length < 3 || parts[0] != "spotify")
            throw new SpotifyIdException("Invalid Spotify URI");

        string itemTypeStr = parts[1] == "user" ? parts[3] : parts[1];
        string id = parts[^1];

        //spotify:meta:page
        SpotifyItemType itemType = itemTypeStr switch
        {
            "album" => SpotifyItemType.Album,
            "artist" => SpotifyItemType.Artist,
            "episode" => SpotifyItemType.Episode,
            "playlist" => SpotifyItemType.Playlist,
            "show" => SpotifyItemType.Show,
            "track" => SpotifyItemType.Track,
            "local" => SpotifyItemType.Local,
            "folder" => SpotifyItemType.FolderUnknown,
            "start-group" => SpotifyItemType.FolderStart,
            "end-group" => SpotifyItemType.FolderEnd,
            "meta" => parts[2] == "page" ? SpotifyItemType.MetaPage : throw new SpotifyIdException("Invalid ID"),
            _ => SpotifyItemType.Unknown
        };

        if (itemType == SpotifyItemType.Local)
        {
            //:Lee+Seung+Gi:The+Dream+Of+A+Moth:%EB%82%B4+%EC%97%AC%EC%9E%90%EB%9D%BC%EB%8B%88%EA%B9%8C+%28+Because+You%27re+My+Girl%29:24
            var everythingAfter = parts[2..];
            ReadOnlySpan<byte> idBytes = Encoding.UTF8.GetBytes(string.Join(":", everythingAfter));
            var idBigInt = new BigInteger(idBytes);
            return new SpotifyId(idBigInt, itemType);
        }

        if (itemType is SpotifyItemType.FolderUnknown or SpotifyItemType.FolderEnd or SpotifyItemType.FolderStart)
        {
            // id as biginteger
            //id is actually after start-group or end-group
            id = parts[2];
            var idBytes = Encoding.UTF8.GetBytes(id);
            var idBigInt = new BigInteger(idBytes);
            return new SpotifyId(idBigInt, itemType);
        }

        if (itemType == SpotifyItemType.MetaPage)
        {
            //spotify:meta:page:{index}
            var pageIndex = int.Parse(id);
            return new SpotifyId(pageIndex, itemType);
        }

        if (id.Length != SIZE_BASE62)
            throw new SpotifyIdException("Invalid ID");

        SpotifyId spotifyId = FromBase62(id);
        return new SpotifyId(spotifyId.Id, itemType);
    }

    public string ToUri()
    {
        if (ItemType is SpotifyItemType.MetaPage)
            return $"spotify:meta:page:{Id}";
        string itemType = ItemType switch
        {
            SpotifyItemType.Album => "album",
            SpotifyItemType.Artist => "artist",
            SpotifyItemType.Episode => "episode",
            SpotifyItemType.Playlist => "playlist",
            SpotifyItemType.Show => "show",
            SpotifyItemType.Track => "track",
            SpotifyItemType.Local => "local",
            SpotifyItemType.FolderUnknown => "folder",
            SpotifyItemType.FolderStart => "start-group",
            SpotifyItemType.FolderEnd => "end-group",
            _ => "unknown"
        };

        return $"spotify:{itemType}:{ToBase62()}";
    }

    public override string ToString() => ToUri();

    public string GetIdentifier()
    {
        return ToString();
    }

    public override bool Equals(object obj) => obj is SpotifyId other && Equals(other);

    public bool Equals(SpotifyId other) => Id == other.Id && ItemType == other.ItemType;

    public override int GetHashCode() => HashCode.Combine(Id, ItemType);

    public static bool operator ==(SpotifyId left, SpotifyId right) => left.Equals(right);

    public static bool operator !=(SpotifyId left, SpotifyId right) => !(left == right);

    public static SpotifyId FromGid(ByteString gid, SpotifyItemType type)
    {
        return FromRaw(gid.Span, type);
    }
}
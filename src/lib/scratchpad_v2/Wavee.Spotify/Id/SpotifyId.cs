using System.Numerics;
using System.Text;
using Wavee.Spotify.Helpers;
using Wavee.Spotify.Id;

namespace Wavee.Spotify.Clients.Info;

/// <summary>
///     A Spotify URI is a resource identifier that can be entered in the Spotify desktop client's search box to locate an
///     artist, album, or track.
///     It has the format spotify:{type}:{id}, where {type} is the type of resource and {id} is the base-62 identifier for
///     the resource.
///     <br /> <br />
///     The final uri will be built by converting the id to base-16 and then prepending the type and a colon with the
///     prefix spotify:.
/// </summary>
/// <remarks>
///     For example, the following is a Spotify URI for a track:
///     spotify:track:6rqhFgbbKwnb9MLmUQDhG6
/// </remarks>
/// <param name="Id">
///     A <see cref="BigInteger" /> is used for the Id property of the <see cref="SpotifyId" /> struct because
///     Spotify IDs are base-62 encoded strings that can be up to 22 characters long. <br /> The maximum value that can be
///     represented by a 22 character base-62 string is 62^22 - 1, which is a very large number that exceeds the maximum
///     value that can be stored in a 64-bit integer. A BigInteger provides a way to store and manipulate large integer
///     values that exceed the capacity of a 64-bit integer.
/// </param>
/// <param name="ItemType">The type of the audio item represented by this SpotifyId</param>
public readonly record struct SpotifyId(BigInteger Id, AudioItemType ItemType)
{
    private const uint SIZE = 16;
    private const uint SIZE_BASE16 = 32;
    private const uint SIZE_BASE62 = 22;

    private static readonly byte[] BASE62_DIGITS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        .Select(c => (byte)c).ToArray();

    private static readonly byte[] BASE16_DIGITS = "0123456789abcdef".Select(c => (byte)c).ToArray();

    private static readonly int[] SHIFT_CONSTANTS = { 96, 64, 32, 0 };

    private readonly string? _userIdOverride;

    private SpotifyId(string userId, AudioItemType itemType) : this(BigInteger.Zero, itemType)
    {
        var s = userId.SplitLines();
        s.MoveNext(); // spotify:
        s.MoveNext(); // user:
        s.MoveNext(); // user id
        _userIdOverride = s.Current.Line.ToString();
        ItemType = itemType;
    }

    public override string ToString()
    {
        return ToUri();
    }

    public string IdStr => ToBase62();

    public AudioItemType Type => ItemType;

    /// Returns whether this `SpotifyId` is for a playable audio item, if known.
    public bool IsPlayable()
    {
        return ItemType == AudioItemType.Episode || ItemType == AudioItemType.Track;
    }

    public static SpotifyId FromUser(string userId)
    {
        return new SpotifyId(userId, AudioItemType.User);
    }

    public static SpotifyId FromRaw(ReadOnlySpan<byte> trackGid, Option<AudioItemType> audioItemType)
    {
        var idBytes = new byte[SIZE];
        trackGid.Slice((int)(trackGid.Length - SIZE),
                (int)SIZE)
            .CopyTo(idBytes);

        var id = new SpotifyId
        {
            Id = new BigInteger(idBytes, true, true),
            ItemType = audioItemType.IfNone(AudioItemType.Unknown)
        };

        return id;
    }


    /// <summary>
    ///     Parses a base62 encoded [Spotify ID] into a `u128`.
    ///     [Spotify ID]: https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids
    /// </summary>
    /// <param name="src">Expected to be 22 bytes long and encoded using valid characters.</param>
    /// <returns></returns>
    /// <exception cref="AudioIdException"></exception>
    public static SpotifyId FromBase62(ReadOnlySpan<char> src)
    {
        var dst = BigInteger.Zero;
        foreach (var cr in src)
        {
            var c = cr;
            var p = c switch
            {
                var b when b >= '0' && b <= '9' => new BigInteger((byte)(c - '0')),
                var b when b >= 'a' && b <= 'z' => new BigInteger((byte)(c - 'a' + 10)),
                var b when b >= 'A' && b <= 'Z' => new BigInteger((byte)(c - 'A' + 36)),
                _ => throw new AudioIdException(SpotifyIdError.InvalidId, src)
            };

            dst *= 62;
            dst += p;
        }

        return new SpotifyId(dst, AudioItemType.Unknown);
    }

    /// <summary>
    ///     Parses a [Spotify URI] into a `SpotifyId`.
    ///     Note that this should not be used for playlists, which have the form of
    ///     `spotify:playlist:{id}`.
    ///     [Spotify URI]: https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids
    /// </summary>
    /// <param name="src">
    ///     Expected to be in the canonical form `spotify:{type}:{id}`, where `{type}`
    ///     can be arbitrary while `{id}` is a 22-character long, base62 encoded Spotify ID.
    /// </param>
    /// <returns></returns>
    /// <exception cref="AudioIdException"></exception>
    public static SpotifyId FromUri(string src)
    {
        // Basic: `spotify:{type}:{id}`
        // Named: `spotify:user:{user}:{type}:{id}`
        // Local: `spotify:local:{artist}:{album_title}:{track_title}:{duration_in_seconds}`
        var partsEnumerator = src.SplitLines();

        partsEnumerator.MoveNext();
        var scheme = partsEnumerator.Current.Line;

        partsEnumerator.MoveNext();

        var itemType = partsEnumerator.Current.Line;

        ReadOnlySpan<char> id = default;
        while (partsEnumerator.MoveNext())
            id = partsEnumerator.Current.Line;

        if (scheme is not "spotify")
            throw new AudioIdException(SpotifyIdError.InvalidRoot, src);

        var enumType = (AudioItemType)Enum.Parse(typeof(AudioItemType), itemType, true);
        switch (enumType)
        {
            // Local files have a variable-length ID: https://developer.spotify.com/documentation/general/guides/local-files-spotify-playlists/
            // TODO: find a way to add this local file ID to SpotifyId.
            // One possible solution would be to copy the contents of `id` to a new String field in SpotifyId, and then parse it in the FromUri method.
            case AudioItemType.Local:
                return new SpotifyId(0, enumType);
            case AudioItemType.User:
                return new SpotifyId(id.ToString(), AudioItemType.User);
        }

        if (id.Length != 22)
            throw new AudioIdException(SpotifyIdError.InvalidId, src);

        var item = FromBase62(id) with
        {
            ItemType = enumType
        };
        return item;
    }

    // The algorithm is based on:
    // https://github.com/trezor/trezor-crypto/blob/c316e775a2152db255ace96b6b65ac0f20525ec0/base58.c
    //
    // We are not using naive division of id as it is an u64 and div + mod are software
    // emulated at runtime (and unoptimized into mul + shift) on non-128bit platforms,
    // making them very expensive.
    //
    // Trezor's algorithm allows us to stick to arithmetic on native registers making this
    // an order of magnitude faster. Additionally, as our sizes are known, instead of
    // dealing with the ID on a byte by byte basis, we decompose it into four u32s and
    // use 64-bit arithmetic on them for an additional speedup.
    public string ToBase62()
    {
        Span<byte> dst = new byte[22];
        var i = 0;
        var n = Id;

        foreach (var shift in SHIFT_CONSTANTS)
        {
            /*
             * //initial shift
            var x = n >> shift;
            const uint mask = 0xFFFFFFFFu;
            var y = x & mask;
            //now cast back to ulong
            ulong carry = y;
             */

            //In Rust, casting a large integer to u32 performs a truncation of the most significant bits,
            //  effectively discarding all but the 32 least significant bits. This means that the resulting value may be different from the original
            //  but it will always fit within the range of a 32-bit unsigned integer.
            //
            //In C#, on the other hand, when you cast a large integer to an unsigned type that's smaller than the original type,
            //  you'll get an exception if the original value doesn't fit within the range of the target type.
            //
            //To fix the issue, we can perform the truncation explicitly by applying a bitwise AND operation with the mask 0xFFFFFFFFu.
            //  This will ensure that the resulting value fits within the range of a 32-bit unsigned integer.

            //The above code is equivalent to the following line:
            ulong carry = (uint)((n >> shift) & 0xFFFFFFFFu);
            for (var index = 0; index < dst[..i].Length; index++)
            {
                var b = dst[..i][index];
                //                carry += (*b as u64) << 32;
                var carry2 = (ulong)b << 32;
                carry += carry2;

                dst[..i][index] = (byte)(carry % 62);
                carry /= 62;
            }

            while (carry > 0)
            {
                dst[i] = (byte)(carry % 62);
                carry /= 62;
                i += 1;
            }
        }

        for (var j = 0; j < dst.Length; j++) dst[j] = BASE62_DIGITS[dst[j]];

        dst.Reverse();
        var id =
            Encoding.UTF8.GetString(dst);
        return id;
    }

    public string ToBase16()
    {
        //        to_base16(&self.to_raw(), &mut [0u8; Self::SIZE_BASE16])
        Span<byte> buf = new byte[SIZE_BASE16];
        return ToBase16(ToRaw(), buf);
    }

    public static string ToBase16(ReadOnlySpan<byte> src, Span<byte> buf)
    {
        var i = 0;
        foreach (var v in src)
        {
            buf[i] = BASE16_DIGITS[v >> 4];
            buf[i + 1] = BASE16_DIGITS[v & 0x0f];
            i += 2;
        }

        return Encoding.UTF8.GetString(buf);
    }

    /// Returns a copy of the `SpotifyId` as an array of `SpotifyId::SIZE` (16) bytes in
    /// big-endian order.
    public byte[] ToRaw()
    {
        ReadOnlySpan<byte> id = Id.ToByteArray(true, true);
        var padding = (int)SIZE - id.Length;
        Span<byte> temp = new byte[SIZE];
        id.CopyTo(temp[padding..]);
        return temp.ToArray();
    }


    /// Returns the `SpotifyId` as a [Spotify URI] in the canonical form `spotify:{type}:{id}`,
    /// where `{type}` is an arbitrary string and `{id}` is a 22-character long, base62 encoded
    /// Spotify ID.
    /// 
    /// If the `SpotifyId` has an associated type unrecognized by the library, `{type}` will
    /// be encoded as `unknown`.
    /// 
    /// [Spotify URI]: https://developer.spotify.com/documentation/web-api/#spotify-uris-and-ids
    public string ToUri()
    {
        if (!string.IsNullOrEmpty(_userIdOverride))
            return $"spotify:user:{_userIdOverride}";
        var item_type = ItemType.ToString().ToLower();
        var base62 = ToBase62();
        return $"spotify:{item_type}:{base62}";
    }

    public string ToId()
    {
        return _userIdOverride ?? ToBase62();
    }

    public static AudioItemType InferUriPrefix(string contextUri)
    {
        if (contextUri.StartsWith("spotify:episode:") || contextUri.StartsWith("spotify:show:"))
            return AudioItemType.Episode;
        return AudioItemType.Track;
    }
}
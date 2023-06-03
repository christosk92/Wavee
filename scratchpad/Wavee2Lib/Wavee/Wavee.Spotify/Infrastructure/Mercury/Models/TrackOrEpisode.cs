using System.Numerics;
using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;
using static LanguageExt.Prelude;
namespace Wavee.Spotify.Infrastructure.Mercury.Models;

public class TrackOrEpisode
{
    public TrackOrEpisode(Either<Episode, Lazy<Track>> Value)
    {
        this.Value = Value;
    }

    public string Name => Value.Match(
        Left: e => e.Name,
        Right: t => t.Value.Name
    );


    public AudioId Id => Value.Match(
        Left: episode => AudioId.FromRaw(episode.Gid.Span, AudioItemType.PodcastEpisode, ServiceType.Spotify),
        Right: track => AudioId.FromRaw(track.Value.Gid.Span, AudioItemType.Track, ServiceType.Spotify)
    );

    public TimeSpan Duration => Value.Match(
               Left: e => TimeSpan.FromMilliseconds(e.Duration),
               Right: t => TimeSpan.FromMilliseconds(t.Value.Duration)
        );

    public Seq<SpotifyTrackArtist> Artists => Value.Match(
        Left: e => LanguageExt.Seq<SpotifyTrackArtist>.Empty,
        Right: t => t.Value.ArtistWithRole.Select(SpotifyTrackArtist.From).Cast<SpotifyTrackArtist>().ToSeq()
    );

    public SpotifyTrackAlbum Group => Value.Match(
        Left: e => SpotifyTrackAlbum.From(e.Show),
        Right: t => SpotifyTrackAlbum.From(t.Value.Album, t.Value.DiscNumber)
    );

    public Either<Episode, Lazy<Track>> Value { get; init; }
    public Track AsTrack => Value.Match(
               Left: e => throw new InvalidOperationException("This is an episode"),
               Right: t => t.Value
               );

    public bool CanPlay => true;

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

    public string GetImage(Image.Types.Size size)
    {
        const string cdnUrl = "https://i.scdn.co/image/{0}";
        var res = Value.Match(
                Left: e =>
                {
                    var r = e.CoverImage.Image.SingleOrDefault(c => c.Size == size);
                    if (r is not null)
                        return Some(r);
                    return e.CoverImage.Image.First();
                },
                Right: t =>
                {
                    if (t.Value.Album.CoverGroup?.Image is null) return None;
                    var r = t.Value.Album.CoverGroup.Image.SingleOrDefault(c => c.Size == size);
                    if (r is not null)
                        return Some(r);
                    return Some(t.Value.Album.CoverGroup.Image.First());
                })
            .Bind(img =>
            {
                var base16 = ToBase16(img.FileId.Span);
                return Some(string.Format(cdnUrl, base16));
            });
        return res.ValueUnsafe();
    }

    public static string ToBase16(ReadOnlySpan<byte> raw)
    {
        //convert to hex
        var hex = new StringBuilder(raw.Length * 2);
        foreach (var b in raw)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public void Deconstruct(out Either<Episode, Lazy<Track>> Value)
    {
        Value = this.Value;
    }
}
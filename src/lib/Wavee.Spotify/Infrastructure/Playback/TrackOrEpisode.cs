using System.Numerics;
using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.Playback;

public readonly record struct TrackOrEpisode(Either<Episode, Track> Value)
{
    static TrackOrEpisode()
    {
        FormatsMap = new HashMap<PreferredQualityType, AudioFile.Types.Format[]>(new[]
        {
            (PreferredQualityType.Low, new[]
            {
                AudioFile.Types.Format.OggVorbis96,
                AudioFile.Types.Format.Mp396,
                AudioFile.Types.Format.Mp3160
            }),
            (PreferredQualityType.Normal, new[]
            {
                AudioFile.Types.Format.OggVorbis160,
                AudioFile.Types.Format.Mp3160,

                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3256,

                AudioFile.Types.Format.Aac48,
                AudioFile.Types.Format.FlacFlac,

                AudioFile.Types.Format.Mp396,
                AudioFile.Types.Format.OggVorbis96,
            }),
            (PreferredQualityType.High, new[]
            {
                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3320
            }),
            (PreferredQualityType.Highest, new[]
            {
                AudioFile.Types.Format.FlacFlac,
                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3256,
                AudioFile.Types.Format.Aac48,
            })
        });
    }

    public Option<AudioFile> FindFile(PreferredQualityType quality)
    {
        return Value.Match(
            Left: e =>
            {
                return e.Audio
                    .Find(f =>
                    {
                        var r = FormatsMap.Find(quality).Map(x => x.Contains(f.Format));
                        return r.Match(
                            Some: t => t,
                            None: () => false
                        );
                    });
            },
            Right: t =>
            {
                return t.File
                    .Find(f =>
                    {
                        var r = FormatsMap.Find(quality).Map(x => x.Contains(f.Format));
                        return r.Match(
                            Some: t => t,
                            None: () => false
                        );
                    });
            }
        );
    }

    public Option<AudioFile> FindAlternativeFile(PreferredQualityType quality)
    {
        return Value.Match(
            Left: episode => None,
            Right: track =>
            {
                var alt = track.Alternative
                    .Fold(Option<AudioFile>.None, (files, track1) =>
                    {
                        return track1.File.Find(f =>
                        {
                            var r = FormatsMap.Find(quality).Map(x => x.Contains(f.Format));
                            return r.Match(
                                Some: t => t,
                                None: () => false
                            );
                        });
                    });
                return alt;
            }
        );
    }

    private static HashMap<PreferredQualityType, AudioFile.Types.Format[]> FormatsMap { get; }

    // public AudioId Id => Value.Match(
    //     Left: e => e.ToId(),
    //     Right: t => t.ToId()
    // );

    public string Name => Value.Match(
        Left: e => e.Name,
        Right: t => t.Name
    );


    public AudioId Id => Value.Match(
        Left: episode => AudioId.FromRaw(episode.Gid.Span, AudioItemType.PodcastEpisode, ServiceType.Spotify),
        Right: track => AudioId.FromRaw(track.Gid.Span, AudioItemType.Track, ServiceType.Spotify)
    );

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
                    var r = t.Album.CoverGroup.Image.SingleOrDefault(c => c.Size == size);
                    if (r is not null)
                        return Some(r);
                    return Some(t.Album.CoverGroup.Image.First());
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
}
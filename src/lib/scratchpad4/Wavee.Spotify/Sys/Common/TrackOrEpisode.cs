using System.Text;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Spotify.Sys.Playback;

namespace Wavee.Spotify.Sys.Common;

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
            (PreferredQualityType.Default, new[]
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
                AudioFile.Types.Format.OggVorbis320,
                AudioFile.Types.Format.Mp3256,
                AudioFile.Types.Format.Aac48,
                AudioFile.Types.Format.FlacFlac
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
        // var track = Track;
        // var episode = Episode;
        // return FormatsMap
        //     .Find(quality)
        //     .Bind(formats => track.Match(
        //         Some: t => t.File.Find(f => formats.Contains(f.Format)),
        //         None: () => episode.Match(
        //             Some: e => e.Audio.Find(f => formats.Contains(f.Format)),
        //             None: () => None
        //         )
        //     ));
    }

    public Option<AudioFile> FindAlternativeFile(PreferredQualityType quality)
    {
        throw new NotImplementedException();
    }

    private static HashMap<PreferredQualityType, AudioFile.Types.Format[]> FormatsMap { get; }

    public string Name => Value.Match(
        Left: e => e.Name,
        Right: t => t.Name
    );

    public int Duration => Value.Match(
        Left: e => e.Duration,
        Right: t => t.Duration
    );

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
    private static readonly byte[] BASE16_DIGITS = "0123456789abcdef".Select(c => (byte)c).ToArray();
    private static string ToBase16(ReadOnlySpan<byte> fileIdSpan)
    {
        Span<byte> buffer = new byte[40];
        var i = 0;
        foreach (var v in fileIdSpan)
        {
            buffer[i] = BASE16_DIGITS[v >> 4];
            buffer[i + 1] = BASE16_DIGITS[v & 0x0f];
            i += 2;
        }

        return Encoding.UTF8.GetString(buffer);
    }

}
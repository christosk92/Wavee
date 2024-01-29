using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Spfy.Items;

namespace Wavee.Spfy.Mapping;

internal static class AlbumMapping
{
    public static SpotifySimpleAlbum MapToDto(this Album album)
    {
        return new SpotifySimpleAlbum
        {
            Uri = SpotifyId.FromRaw(album.Gid.Span,
                AudioItemType.Album),
            Name = album.Name,
            Images = album.CoverGroup.Image.Select(x => x.MapToDto())
                .ToSeq(),
            Year = album.Date.Year,
            Type = album.Type.ToString(),
            Tracks = album.Disc.SelectMany(x => x.Track.Select(t => (SpotifyId.FromRaw(t.Gid.Span,
                    AudioItemType.Track), (ushort)x.Number)))
                .ToSeq(),
            Artists = album.Artist.Select(x => new WaveePlayableItemDescription
            {
                Name = x.Name,
                Id = SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Artist).ToString()
            }).Cast<IWaveeAlbumArtist>().ToSeq()
        };
    }
    //MapToGroup
    public static ISpotifyPlayableItemGroup MapToGroup(this Album album, Track fromTrack)
    {
        if (album is null)
        {
            return null;
        }

        var res = new SpotifyTrackAlbum
        {
            Uri = SpotifyId.FromRaw(album.Gid.Span,
                AudioItemType.Album),
            Name = album.Name,
            Images = album.CoverGroup.Image.Select(x => x.MapToDto())
                .ToSeq(),
            Year = album.Date.Year,
            Artists =
                new[]
                {
                    new WaveePlayableItemDescription
                    {
                        Name = fromTrack.Artist.First()
                            .Name,
                        Id = SpotifyId.FromRaw(fromTrack.Artist.First()
                                    .Gid.Span,
                                AudioItemType.Artist)
                            .ToString()
                    }
                }.ToSeq(),
            Number = FindTrackNumber(album,
                    fromTrack)
                .Number,
            Playcount = 0,
            Duration = TimeSpan.FromMilliseconds(fromTrack.Duration),
            Uid = null
        };

        return res;
    }

    private static (int Disc, int Number) FindTrackNumber(Album album, Track fromTrack)
    {
        int x = 1;
        foreach (var disc in album.Disc)
        {
            int y = 1;
            foreach (var b in disc.Track)
            {
                if (b.Gid.SequenceEqual(fromTrack.Gid)) return (x, y);
                y++;
            }

            x++;
        }

        return (0, 0);
    }
}
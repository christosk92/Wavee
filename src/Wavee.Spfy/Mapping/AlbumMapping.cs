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
            Artists = album.Artist.Select(x => new SpotifyPlayableItemDescription
            {
                Name = x.Name,
                Uri = SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Artist)
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
                    new SpotifyPlayableItemDescription
                    {
                        Name = fromTrack.Artist.First().Name,
                        Uri = SpotifyId.FromRaw(fromTrack.Artist.First().Gid.Span, AudioItemType.Artist)
                    }
                }.ToSeq()
        };

        return res;
    }
}
using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Spfy.Items;

namespace Wavee.Spfy.Mapping;

internal static class ArtistMapping
{
    public static SpotifySimpleArtist MapToDto(this Artist artist)
    {
        return new SpotifySimpleArtist
        {
            Uri = SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist),
            Name = artist.Name,
            Images = artist.PortraitGroup.Image.Select(x => x.MapToDto())
                .ToSeq(),
            Discography = BuildDiscography(artist)
        };
    }

    public static WaveePlayableItemDescription MapToDescription(this Artist artist)
    {
        return new WaveePlayableItemDescription
        {
            Id = SpotifyId.FromRaw(artist.Gid.Span,
                    AudioItemType.Artist)
                .ToString(),
            Name = artist.Name,
            Type = AudioItemType.Artist
        };
    }


    private static IEnumerable<IGrouping<SpotifyDiscographyType, SpotifyId>> BuildDiscography(Artist artist)
    {
        var albums = artist.AlbumGroup.SelectMany(f => f.Album).Select(x =>
            (SpotifyDiscographyType.Album, SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Album)));
        var singles = artist.SingleGroup.SelectMany(f => f.Album).Select(x =>
            (SpotifyDiscographyType.Single, SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Album)));
        var compilations = artist.CompilationGroup.SelectMany(f => f.Album).Select(x =>
            (SpotifyDiscographyType.Compilation, SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Album)));


        return albums
            .Concat(singles)
            .Concat(compilations)
            .GroupBy(x => x.Item1, x => x.Item2);
    }
}
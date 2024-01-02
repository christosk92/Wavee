using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Metadata;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Mappings;

internal static class AlbumMapping
{
    public static SpotifySimpleAlbum MapToDto(this Album album)
    {
        return new SpotifySimpleAlbum
        {
            Uri = default
        };
    }
    //MapToGroup
    public static SpotifyPlayableItemGroup MapToGroup(this Album album)
    {
        return new SpotifyPlayableItemGroup
        {
            Uri = SpotifyId.FromRaw(album.Gid.Span, AudioItemType.Album),
            Name = album.Name,
            Images = album.CoverGroup.Image.Select(f=> f.MapToDto()).ToImmutableArray()
        };
    }
}
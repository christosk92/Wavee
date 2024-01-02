using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Metadata;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Mappings;

internal static class ArtistMapping
{
    public static SpotifySimpleArtist MapToDto(this Artist artist)
    {
        return new SpotifySimpleArtist
        {
            Uri = default
        };
    }
    
    public static SpotifyPlayableItemDescription MapToDescription(this Artist artist)
    {
        return new SpotifyPlayableItemDescription
        {
            Uri = SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist),
            Name = artist.Name
        };
    }
}
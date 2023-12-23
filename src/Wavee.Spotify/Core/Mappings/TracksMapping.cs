using Spotify.Metadata;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Mappings;

internal static class TracksMapping
{
    public static SpotifyTrack MapToDto(this Track track)
    {
        return new SpotifyTrack();
    }
}
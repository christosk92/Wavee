using Spotify.Metadata;
using Wavee.Spotify.Core.Models.Metadata;

namespace Wavee.Spotify.Core.Mappings;

internal static class EpisodeMapping
{
    public static SpotifySimpleEpisode MapToDto(this Episode episode)
    {
        return new SpotifySimpleEpisode
        {
            Uri = default
        };
    }
}
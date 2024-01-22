using Spotify.Metadata;
using Wavee.Spfy.Items;

namespace Wavee.Spfy.Mapping;

internal static class EpisodeMapping
{
    public static SpotifySimpleEpisode MapToDto(this Episode episode)
    {
        return new SpotifySimpleEpisode
        {
            Uri = default,
            Name = episode.Name,
            Duration = default,
            Images = default,
            Description = null
        };
    }
    
    public static SpotifySimpleShow MapToDto(this Show show)
    {
        return new SpotifySimpleShow
        {
            Uri = default
        };
    }
}
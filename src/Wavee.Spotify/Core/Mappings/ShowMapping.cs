using Spotify.Metadata;
using Wavee.Spotify.Core.Models.Metadata;

namespace Wavee.Spotify.Core.Mappings;

internal static class ShowMapping
{
    public static SpotifySimpleShow MapToDto(this Show show)
    {
        return new SpotifySimpleShow
        {
            Uri = default
        };
    }
}
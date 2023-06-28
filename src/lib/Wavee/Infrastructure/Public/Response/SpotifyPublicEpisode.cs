using Wavee.Id;

namespace Wavee.Infrastructure.Public.Response;

public class SpotifyPublicEpisode
{
    public required string PreviewUrl { get; init; }
    public required SpotifyId TrackUri { get; init; }
}
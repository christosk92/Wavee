using Wavee.Id;

namespace Wavee.Infrastructure.Public.Response;

public sealed class SpotifyPublicTrack
{
    public required string PreviewUrl { get; init; }
    public required SpotifyId TrackUri { get; init; }
}
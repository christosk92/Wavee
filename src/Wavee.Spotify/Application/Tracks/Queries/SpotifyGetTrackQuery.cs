using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.Tracks.Queries;

public sealed class SpotifyGetTrackQuery : IQuery<Track>
{
    public required SpotifyId TrackId { get; init; } = default!;
}
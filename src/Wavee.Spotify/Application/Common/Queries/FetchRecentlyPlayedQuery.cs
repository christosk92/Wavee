using Mediator;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Application.Common.Queries;

public sealed class FetchRecentlyPlayedQuery : IQuery<IReadOnlyCollection<SpotifyRecentlyPlayedItem>>
{
    public required string User { get; init; }
    public required int Limit { get; init; }
    public required string Filter { get; init; }
}
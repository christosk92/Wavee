using System.Collections.ObjectModel;
using Mediator;
using Spotify.Metadata;

namespace Wavee.UI.Features.Tracks;

public sealed class GetTracksMetadataRequest : IRequest<IReadOnlyDictionary<string, TrackOrEpisode?>>
{
    public required IReadOnlyCollection<string> Ids { get; init; }
    public required IReadOnlyCollection<string> SearchTerms { get; init; }
}

public readonly record struct TrackOrEpisode(Track? Track, Episode? Episode);
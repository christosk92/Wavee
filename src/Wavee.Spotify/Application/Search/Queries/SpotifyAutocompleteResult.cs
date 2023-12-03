using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.Search.Queries;

public sealed class SpotifyAutocompleteResult
{
    public required IReadOnlyCollection<SpotifyAutocompleteHit> Hits { get; set; }
    public required IReadOnlyCollection<SpotifyAutocompleteQuery> Queries{ get; init; }
}

public sealed class SpotifyAutocompleteHit
{
    public required SpotifyId Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
}

public class SpotifyAutocompleteQuery
{
    public required string Query { get; init; }
    public required IReadOnlyCollection<SpotifyAutocompleteQuerySegment> Segments { get; init; }
}

public sealed class SpotifyAutocompleteQuerySegment
{
    public required string Value { get; init; }
    public required bool Matched { get; init; }
}
using System.Collections.Immutable;

namespace Wavee.Spfy.Items;

public readonly record struct SpotifySimpleArtist : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }

    public string Id => Uri.ToString();

    public required ImmutableArray<UrlImage> Images { get; init; }
    public required IEnumerable<IGrouping<SpotifyDiscographyType, SpotifyId>>? Discography { get; init; }
}

public enum SpotifyDiscographyType
{
    Album,
    Single,
    Compilation
}
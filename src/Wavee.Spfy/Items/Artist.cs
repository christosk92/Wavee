using System.Collections.Immutable;
using LanguageExt;

namespace Wavee.Spfy.Items;

public readonly record struct SpotifySimpleArtist : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }

    public string Id => Uri.ToString();

    public AudioItemType Type => AudioItemType.Artist;

    public required Seq<UrlImage> Images { get; init; }
    public required IEnumerable<IGrouping<SpotifyDiscographyType, SpotifyId>>? Discography { get; init; }
}

public enum SpotifyDiscographyType
{
    Album,
    Single,
    Compilation
}
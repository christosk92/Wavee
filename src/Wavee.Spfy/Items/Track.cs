using System.Collections.Immutable;
using LanguageExt;

namespace Wavee.Spfy.Items;

public readonly record struct SpotifySimpleTrack : ISpotifyPlayableItem
{
    public required SpotifyId Uri { get; init; }

    public Seq<UrlImage> Images => Group.Images;

    public required string Name { get; init; }
    public required uint DiscNumber { get; init; }
    public required uint TrackNumber { get; init; }
    public required Seq<SpotifyPlayableItemDescription> Descriptions { get; init; }
    public required ISpotifyPlayableItemGroup Group { get; init; }
    public required Seq<SpotifyAudioFile> AudioFiles { get; init; }
    public required Seq<SpotifyAudioFile> PreviewFiles { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Id => Uri.ToString();
    public required bool Explicit { get; init; }

}

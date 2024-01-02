using System.Collections.Immutable;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Metadata;

public readonly struct SpotifySimpleTrack : ISpotifyPlayableItem
{
    public required SpotifyId Uri { get; init; }
    public required string Title { get; init; }
    public required uint DiscNumber { get; init; }
    public required uint TrackNumber { get; init; }
    public required ImmutableArray<SpotifyPlayableItemDescription> Descriptions { get; init; }
    public required SpotifyPlayableItemGroup Group { get; init; }
    public required ImmutableArray<SpotifyAudioFile> AudioFiles { get; init; }
    public required ImmutableArray<SpotifyAudioFile> PreviewFiles { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Id => Uri.ToString();
    public required bool Explicit { get; init; }
}
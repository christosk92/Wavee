using System.Collections.Immutable;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Metadata;

public readonly struct SpotifySimpleEpisode : ISpotifyPlayableItem
{
    public required SpotifyId Uri { get; init; }
    public string Title { get; }
    public ImmutableArray<SpotifyPlayableItemDescription> Descriptions { get; }
    public SpotifyPlayableItemGroup Group { get; }
    public ImmutableArray<SpotifyAudioFile> AudioFiles { get; }
    public ImmutableArray<SpotifyAudioFile> PreviewFiles { get; }
    public TimeSpan Duration { get; }
    public string? Id => Uri.ToString();
    public bool Explicit { get; }
}
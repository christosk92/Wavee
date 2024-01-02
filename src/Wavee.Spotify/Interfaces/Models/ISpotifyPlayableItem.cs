using System.Collections.Immutable;
using Wavee.Interfaces.Models;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Interfaces.Models;

public interface ISpotifyPlayableItem : ISpotifyItem, IWaveePlayableItem
{
    string Title { get; }
    ImmutableArray<SpotifyPlayableItemDescription> Descriptions { get; }
    SpotifyPlayableItemGroup Group { get; }
    
    ImmutableArray<SpotifyAudioFile> AudioFiles { get; }
    ImmutableArray<SpotifyAudioFile> PreviewFiles { get; }
    
    TimeSpan Duration { get; }
    bool Explicit { get; }
}
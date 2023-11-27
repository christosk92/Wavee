using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Domain.State;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    IWaveePlayer Player { get; }
    Task Initialize(CancellationToken cancellationToken = default);
    event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;
}
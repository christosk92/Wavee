using Wavee.Spotify.Domain.State;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    Task Initialize(CancellationToken cancellationToken = default);

    event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;
}
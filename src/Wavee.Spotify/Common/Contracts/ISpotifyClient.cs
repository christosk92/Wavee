using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.AudioKeys;
using Wavee.Spotify.Application.StorageResolve;
using Wavee.Spotify.Domain.State;
using Wavee.Spotify.Domain.Tracks;
using Wavee.Spotify.Domain.User;

namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    SpotifyClientConfig Config { get; }
    IWaveePlayer Player { get; }
    ISpotifyTrackClient Tracks { get; }
    ISpotifyAudioKeyClient AudioKeys { get; }
    ISpotifyStorageResolver StorageResolver { get; }
    Task<Me> Initialize(CancellationToken cancellationToken = default);
    event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;
}
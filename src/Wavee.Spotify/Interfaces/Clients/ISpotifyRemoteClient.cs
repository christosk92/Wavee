using Wavee.Spotify.Core.Models.Playback;

namespace Wavee.Spotify.Interfaces.Clients;

public interface ISpotifyRemoteClient
{
    ValueTask<bool> Connect(CancellationToken cancellationToken = default);
    
    IObservable<WaveeSpotifyRemoteState> State { get; }
}
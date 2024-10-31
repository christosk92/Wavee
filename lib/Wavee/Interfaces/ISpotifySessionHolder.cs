using Eum.Spotify;

namespace Wavee.Interfaces;

public interface ISpotifySessionHolder
{
    IObservable<bool> Connected { get; }
    ValueTask<LoginCredentials> EnsureConnectedAsync(bool log, CancellationToken cancellationToken);
}
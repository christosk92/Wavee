namespace Wavee.Spotify.Core.Interfaces.Clients;

public interface ISpotifyRemoteClient
{
    ValueTask<bool> Connect(CancellationToken cancellationToken = default);
}
namespace Wavee.Spotify.Interfaces.Clients;

public interface ISpotifyRemoteClient
{
    ValueTask<bool> Connect(CancellationToken cancellationToken = default);
}
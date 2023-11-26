namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    Task Initialize(CancellationToken cancellationToken = default);
}
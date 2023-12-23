using Eum.Spotify;
using Wavee.Spotify.Core.Models.Credentials;

namespace Wavee.Spotify.Core.Interfaces;

public interface ISpotifyCredentialsRepository
{
    bool TryGetDefault(SpotifyCredentialsType type, out SpotifyStoredCredentialsEntity? credentials);
    bool TryGetFor(string username, SpotifyCredentialsType type, out SpotifyStoredCredentialsEntity? credentials);
    void Store(string username, SpotifyCredentialsType type, SpotifyStoredCredentialsEntity cr);
}

public enum SpotifyCredentialsType
{
    OAuth,
    Full
}
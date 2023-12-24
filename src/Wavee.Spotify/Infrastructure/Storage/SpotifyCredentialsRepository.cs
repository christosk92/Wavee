using System.Text.Json;
using Eum.Spotify;
using Wavee.Spotify.Core.Models.Credentials;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify.Infrastructure.Storage;

internal sealed class SpotifyCredentialsRepository : ISpotifyCredentialsRepository
{
    private readonly WaveeSpotifyCredentialsStorageConfig _config;
    public SpotifyCredentialsRepository(WaveeSpotifyConfig config)
    {
        _config = config.CredentialsStorage;
    }
    public bool TryGetDefault(SpotifyCredentialsType type, out SpotifyStoredCredentialsEntity? credentials)
    {
        var defaultUser = _config.GetDefaultUsername();
        if(!string.IsNullOrEmpty(defaultUser))
            return TryGetFor(defaultUser, type, out credentials);
        
        credentials = null;
        return false;
    }

    public bool TryGetFor(string username, SpotifyCredentialsType type, out SpotifyStoredCredentialsEntity? credentials)
    {
        var bytes = _config.OpenCredentials(username, type);
        if (bytes is null)
        {
            credentials = null;
            return false;
        }

        credentials = JsonSerializer.Deserialize<SpotifyStoredCredentialsEntity>(bytes);
        return true;
    }

    public void Store(string username, SpotifyCredentialsType type, SpotifyStoredCredentialsEntity cr)
    {
        _config.SaveCredentials(username, type, JsonSerializer.SerializeToUtf8Bytes(cr));
    }
}

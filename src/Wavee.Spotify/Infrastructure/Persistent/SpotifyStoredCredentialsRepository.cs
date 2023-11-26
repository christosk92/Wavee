using Eum.Spotify;
using Google.Protobuf;
using LiteDB;
using Wavee.Spotify.Application.Authentication.Modules;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Wavee.Spotify.Infrastructure.Persistent;

internal sealed class SpotifyStoredCredentialsRepository : ISpotifyStoredCredentialsRepository
{
    private readonly ILiteCollection<StoredCredentials> _collection;

    public SpotifyStoredCredentialsRepository(ILiteDatabase db)
    {
        _collection = db.GetCollection<StoredCredentials>("credentials");
    }

    public Task<StoredCredentials?> GetStoredCredentials(string? username, CancellationToken cancellationToken)
    {
        var storedCredentials = _collection.FindOne(x => x.Username == username);
        return Task.FromResult(storedCredentials);
    }

    public Task StoreCredentials(StoredCredentials credentials, bool isDefault, CancellationToken cancellationToken)
    {
        _collection.Insert(credentials);
        return Task.CompletedTask;
    }

    public Task<string?> GetDefaultUser(CancellationToken cancellationToken)
    {
        var storedCredentials = _collection.FindOne(x => x.IsDefault);
        return Task.FromResult(storedCredentials?.Username);
    }

    // private async Task<StoredCredentials?> GetStoredCredentials()
    // {
    //     try
    //     {
    //         var storedCredentials = Path.Combine(_config.Storage.Path, "credentials.json");
    //         if (File.Exists(storedCredentials))
    //         {
    //             var json = await File.ReadAllTextAsync(storedCredentials);
    //             var result = JsonSerializer.Deserialize<StoredCredentials>(json);
    //             return result;
    //         }
    //
    //         return null;
    //     }
    //     catch (Exception)
    //     {
    //         return null;
    //     }
    // }
}

public interface ISpotifyStoredCredentialsRepository
{
    Task<StoredCredentials?> GetStoredCredentials(string? username, CancellationToken cancellationToken);
    Task StoreCredentials(StoredCredentials credentials, bool isDefault, CancellationToken cancellationToken);
    Task<string?> GetDefaultUser(CancellationToken cancellationToken);
}
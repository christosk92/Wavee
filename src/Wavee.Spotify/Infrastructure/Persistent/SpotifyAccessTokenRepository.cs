using Eum.Spotify.login5v3;
using LiteDB;

namespace Wavee.Spotify.Infrastructure.Persistent;

internal sealed class SpotifyAccessTokenRepository : ISpotifyAccessTokenRepository
{
    private readonly ILiteCollection<SpotifyAccessToken> _collection;

    public SpotifyAccessTokenRepository(ILiteDatabase liteDatabase)
    {
        _collection = liteDatabase.GetCollection<SpotifyAccessToken>("access_tokens");
    }

    public Task StoreAccessToken(LoginOk ok, CancellationToken cancellationToken)
    {
        var existingTokens = _collection.Find(x => x.Username == ok.Username);
        foreach (var existingToken in existingTokens)
        {
            _collection.Delete(existingToken.Id);
        }

        var accessToken = new SpotifyAccessToken(
            Id: ObjectId.NewObjectId(),
            Value: ok.AccessToken,
            Expiration: DateTimeOffset.UtcNow.AddSeconds(ok.AccessTokenExpiresIn),
            RefreshToken: null,
            Username: ok.Username,
            InstanceId: Constants.InstanceId
        );
        _collection.Insert(accessToken);
        return Task.CompletedTask;
    }

    public Task<SpotifyAccessToken?> GetAccessToken(string requestUsername, CancellationToken cancellationToken)
    {
        var accessToken = _collection.FindOne(x => x.Username == requestUsername);
        if (accessToken == default)
        {
            return Task.FromResult<SpotifyAccessToken?>(null);
        }

        if (accessToken.IsExpired || accessToken.InstanceId != Constants.InstanceId)
        {
            //delete
            _collection.Delete(accessToken.Id);
            return Task.FromResult<SpotifyAccessToken?>(null);
        }

        return Task.FromResult<SpotifyAccessToken?>(accessToken);
    }
}

public interface ISpotifyAccessTokenRepository
{
    Task StoreAccessToken(LoginOk ok, CancellationToken cancellationToken);
    Task<SpotifyAccessToken?> GetAccessToken(string requestUsername, CancellationToken cancellationToken);
}

public readonly record struct SpotifyAccessToken(
    [BsonId] ObjectId Id,
    string Value, DateTimeOffset Expiration,
    string RefreshToken,
    string Username,
    string InstanceId)
{
    private static TimeSpan _expirationOffset = TimeSpan.FromMinutes(5);
    public bool IsExpired => DateTimeOffset.UtcNow > (Expiration - _expirationOffset);
}
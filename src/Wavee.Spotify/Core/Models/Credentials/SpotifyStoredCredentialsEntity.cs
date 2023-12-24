using LiteDB;
using Wavee.Spotify.Interfaces;

namespace Wavee.Spotify.Core.Models.Credentials;

public readonly record struct SpotifyStoredCredentialsEntity(
    [BsonId] ObjectId Id,
    DateTimeOffset Expiration,
    string Username,
    string AuthDataBase64,
    SpotifyCredentialsType Type,
    string InstanceId)
{
    private static TimeSpan _expirationOffset = TimeSpan.FromMinutes(5);
    public bool IsExpired => DateTimeOffset.UtcNow > (Expiration - _expirationOffset);
}
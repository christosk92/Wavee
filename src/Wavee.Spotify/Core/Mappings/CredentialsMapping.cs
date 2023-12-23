using Eum.Spotify;
using Google.Protobuf;
using LiteDB;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Models.Credentials;

namespace Wavee.Spotify.Core.Mappings;

internal static class CredentialsMapping
{
    public static LoginCredentials? ToLoginCredentials(this SpotifyStoredCredentialsEntity? credentialsMaybe)
    {
        if (credentialsMaybe is null)
        {
            return null;
        }

        var credentials = credentialsMaybe.Value;
        return new LoginCredentials
        {
            Username = credentials.Username,
            AuthData = credentials.Type switch
            {
                SpotifyCredentialsType.OAuth => ByteString.CopyFromUtf8(credentials.AuthDataBase64),
                SpotifyCredentialsType.Full => ByteString.FromBase64(credentials.AuthDataBase64),
            },
            Typ = credentials.Type switch
            {
                SpotifyCredentialsType.OAuth => AuthenticationType.AuthenticationSpotifyToken,
                SpotifyCredentialsType.Full => AuthenticationType.AuthenticationStoredSpotifyCredentials,
            }
        };
    }

    public static SpotifyStoredCredentialsEntity ToStoredCredentials(this LoginCredentials credentials)
    {
        return new SpotifyStoredCredentialsEntity(
            Id: ObjectId.NewObjectId(),
            Expiration: credentials.Typ switch
            {
                AuthenticationType.AuthenticationSpotifyToken => DateTimeOffset.UtcNow.AddHours(1),
                AuthenticationType.AuthenticationStoredSpotifyCredentials => DateTimeOffset.MaxValue,
            },
            Username: credentials.Username,
            AuthDataBase64:
            credentials.Typ switch
            {
                AuthenticationType.AuthenticationSpotifyToken => credentials.AuthData.ToStringUtf8(),
                AuthenticationType.AuthenticationStoredSpotifyCredentials => credentials.AuthData.ToBase64(),
            },
            Type: credentials.Typ switch
            {
                AuthenticationType.AuthenticationSpotifyToken => SpotifyCredentialsType.OAuth,
                AuthenticationType.AuthenticationStoredSpotifyCredentials => SpotifyCredentialsType.Full,
            },
            InstanceId: Constants.InstanceId
        );
    }
}
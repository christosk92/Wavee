using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Spotify;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var result = await SpotifyRuntime.Authenticate(loginCredentials, Option<ISpotifyListener>.Some(new SpotifyListener()));
var k = "";

public class SpotifyListener : ISpotifyListener
{
    public Unit OnDisconnected(Option<Error> error)
    {
        throw new NotImplementedException();
    }

    public Unit CountryCodeReceived(string countryCode)
    {
        throw new NotImplementedException();
    }
}
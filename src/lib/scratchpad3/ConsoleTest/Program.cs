using System.Diagnostics;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Spotify;
using static LanguageExt.Prelude;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var result = await SpotifyRuntime.Authenticate(loginCredentials, Option<ISpotifyListener>.Some(new SpotifyListener()));
var k = "";
Console.ReadLine();

public class SpotifyListener : ISpotifyListener
{
    public Unit OnConnected(Guid connectionId)
    {
        Debug.WriteLine($"Connected: {connectionId}");
        return unit;
    }

    public Unit OnDisconnected(Option<Error> error)
    {
        throw new NotImplementedException();
    }

    public Unit CountryCodeReceived(string countryCode)
    {
        throw new NotImplementedException();
    }
}
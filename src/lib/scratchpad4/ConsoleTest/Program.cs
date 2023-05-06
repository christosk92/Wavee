using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ= AuthenticationType.AuthenticationUserPass
};

var info = await SpotifyClient.Authenticate(loginCredentials);
var countryCode = await info.CountryCode();
var k ="";
Console.ReadLine();
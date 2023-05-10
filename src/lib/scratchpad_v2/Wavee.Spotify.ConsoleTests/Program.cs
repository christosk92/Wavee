using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify;
using Wavee.Spotify.Configs;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee Test",
        DeviceType: DeviceType.Chromebook
    )
);
var connection = await SpotifyClient.Create(loginCredentials, config);
var countryCode = (await connection.Info.CountryCode).ValueUnsafe();
Console.ReadLine();
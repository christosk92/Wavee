using Eum.Spotify;
using Google.Protobuf;
using Wavee;
using Wavee.Spotify;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var info = await SpotifyClient.Authenticate(loginCredentials);
var countryCode = await info.CountryCode();
var productInfo = await info.ProductInfo();
var countryCodeAgain = await info.CountryCode();

var player = WaveeCore.Player;

// var remoteInfo = await info.Connect(
//     player: player,
//     config: new SpotifyRemoteConfig(
//         DeviceName: "Wavee",
//         DeviceType.Computer,
//         PreferredQualityType.High
//     ));
// remoteInfo.ClusterChanged.Subscribe((c) =>
// {
//     var k = c.ValueUnsafe();
// });
Console.ReadLine();
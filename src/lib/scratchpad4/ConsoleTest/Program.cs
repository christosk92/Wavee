using System.Diagnostics;
using System.Security.Cryptography;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee;
using Wavee.Spotify;
using Wavee.Spotify.Sys.Mercury;
using Wavee.Spotify.Sys.Playback;
using Wavee.Spotify.Sys.Remote;

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
var remoteInfo = await info.Connect(
    player: player,
    config: new SpotifyRemoteConfig(
        DeviceName: "Wavee",
        DeviceType.Computer,
        PreferredQualityType.High
    ));
remoteInfo.ClusterChanged.Subscribe((c) =>
{
    var k = c.ValueUnsafe();
});
Console.ReadLine();
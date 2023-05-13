using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Wavee.Core.Id;
using Wavee.Spotify.Infrastructure;

var credentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    CachePath: Option<string>.None,
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee Test",
        DeviceType.Computer
    ),
    Playback: new SpotifyPlaybackConfig(
        PreferredQualityType.High,
        Autoplay: true
    )
);

var spotifyCore = await SpotifyClient.Create(credentials, config, Option<ILogger>.None);
spotifyCore.RemoteClient.StateChanged.Subscribe(x =>
{
    Console.WriteLine(x);
});
var track = await spotifyCore.GetTrackAsync("3HGlpIqHEInelCfDZYd0Ki");
var c = Console.ReadLine();
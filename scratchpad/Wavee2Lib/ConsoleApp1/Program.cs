using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Wavee.Player;
using Wavee.Spotify;

WaveePlayer.Instance.StateUpdates.Subscribe(state => { Console.WriteLine(state); });

var c = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

//store caches in appdata/local/wavee
var cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wavee");


var config = new SpotifyConfig(
    remote: new SpotifyRemoteConfig(deviceName: "Wavee", deviceType: DeviceType.Computer),
    playback: new SpotifyPlaybackConfig(
        crossfadeDuration: TimeSpan.FromSeconds(10),
        preferedQuality: PreferredQualityType.VeryHigh),
    cache: new SpotifyCacheConfig(cacheRoot: cacheRoot)
);

var spotifyClient = await SpotifyClient.CreateAsync(config, c);

spotifyClient.Remote.StateUpdates.Subscribe(state =>
{
    if (state.IsSome)
    {
        Console.WriteLine(state);
    }
});
//https://open.spotify.com/track/26hOm7dTtBi0TdpDGl141t?si=63327ca800964207
// const string ctxUri = "spotify:artist:7jFUYMpMUBDL4JQtMZ5ilc";
// const int ctxIndex = 0;
// //await spotifyClient.Remote.Takeover();
// await spotifyClient.Playback.Play(ctxUri, ctxIndex, Option<TimeSpan>.None);
Console.ReadLine();
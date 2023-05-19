using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Wavee.AudioOutput.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Playback;

NAudioOutput.SetAsDefaultOutput();

//C:\Users\chris-pc\Documents\spotify_cache
//var cache = "C:\\Users\\chris-pc\\Documents\\spotify_cache";
var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var cache = Path.Combine(appdata, "spotify_cache");
Directory.CreateDirectory(cache);
var config = new SpotifyConfig(
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee",
        DeviceType: DeviceType.Computer
    ),
    Playback: new SpotifyPlaybackConfig(
        PreferredQualityType.Normal,
        CrossfadeDuration: TimeSpan.FromSeconds(10),
        Autoplay: true
    ),
    Cache: new SpotifyCacheConfig(
        CachePath: cache,
        CacheNoTouchExpiration: TimeSpan.FromDays(1)
    )
);

var credentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};
var client = await SpotifyClient.CreateAsync(credentials, config, CancellationToken.None);
client.RemoteClient.StateChanged
    .Subscribe(x =>
    {
        var trackId = x.TrackUri
            .Map(y => y.ToBase16().ToString());
        Console.WriteLine($"State changed: {x}");
    });
GC.Collect();

await client.PlaybackClient.PlayContext("spotify:album:6lumjI581TEGHeTviSikrm", 0);

Console.Read();
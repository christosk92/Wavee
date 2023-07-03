using System.Globalization;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using LanguageExt;
using Serilog;
using Wavee;
using Wavee.Id;
using Wavee.Player;
using Wavee.Time.Live;

var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var spotifyCacheLocation = Path.Combine(documents, "Wavee");
Directory.CreateDirectory(spotifyCacheLocation);


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Verbose()
    .CreateLogger();

var config = new SpotifyConfig(
    Remote: new SpotifyRemoteConfig(
        deviceName: "Wavee",
        deviceType: DeviceType.Computer
    ),
    Cache: new SpotifyCacheConfig(
        CacheLocation: spotifyCacheLocation,
        MaxCacheSize: Option<long>.None
    ),
    Time: new SpotifyTimeConfig(
        Method: TimeSyncMethod.Melody,
        Option<int>.None
    ),
    Playback: new SpotifyPlaybackConfig(
        preferedQuality: PreferedQuality.High,
        crossfadeDuration: TimeSpan.FromSeconds(10)
    ),
    Locale: new CultureInfo("ko-kr")
);

var player = new WaveePlayer();

var client = new SpotifyClient(player, new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass,
    Username = "",
    AuthData = ByteString.CopyFromUtf8("")
}, config);
var countryCode = await client.Country;
var response = await client.Metadata.GetAlbum(SpotifyId.FromUri("spotify:album:2hEnymoejldpuxSdTnkard"));
var listener = client.Remote.CreateListener().Subscribe(x => { Log.Logger.Information("Remote: {0}", x); });
var tookover = await client.Playback.Takeover();
var c = Console.ReadLine();
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using LanguageExt;
using Serilog;
using Wavee;
using Wavee.Id;
using Wavee.Player;

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
    playback: new SpotifyPlaybackConfig(
        preferedQuality: PreferedQuality.High,
        crossfadeDuration: TimeSpan.FromSeconds(10)
    )
);

var player = new WaveePlayer();

var client = new SpotifyClient(player, new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass,
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD"))
}, config);
var countryCode = await client.Country;
var listener = client.Remote.CreateListener().Subscribe(x => { Log.Logger.Information("Remote: {0}", x); });
var artistId = SpotifyId.FromUri("spotify:artist:0nmQIMXWTXfhgOBdNzhGOs");
//var artist = await client.Metadata.GetArtistOverview(artistId, false);
var tookover = await client.Playback.Takeover();
var c = Console.ReadLine();
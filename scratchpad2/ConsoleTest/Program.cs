using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Serilog;
using Wavee;
using Wavee.Id;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Verbose()
    .CreateLogger();

var config = new SpotifyConfig(
    Remote: new SpotifyRemoteConfig(
        deviceName: "Wavee",
        deviceType: DeviceType.Computer
    )
);

var client = new SpotifyClient(new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass,
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD"))
}, config);
var countryCode = await client.Country;
var listener = client.Remote.CreateListener()
    .Subscribe(x =>
    {
        Log.Logger.Information("Remote: {0}", x);
    });
var c = Console.ReadLine();
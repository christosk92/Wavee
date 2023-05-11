using System.Diagnostics;
using System.Text;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Serilog;
using Wavee;
using Wavee.Spotify;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Configs;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee Test",
        DeviceType: DeviceType.Computer
    ),
    Playback: new SpotifyPlaybackConfig(
        PreferredQualityType.High,
        Autoplay: true
    )
);
var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(log);
});
var logger = loggerFactory.CreateLogger<Program>();
var connection = await SpotifyClient.Create(loginCredentials, config, Option<ILogger>.Some(logger));
var remoteCluster = await connection.Remote.Connect(CancellationToken.None);
remoteCluster.Subscribe(state =>
{
    logger.LogInformation("Remote state: {state}", state);
});

while (true)
{
    try
    {
        var input = Console.ReadLine();

        //commands:
        //-play <uri>
        //-pause
        //-seek <ts>
        var command = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (command[0])
        {
            case "-mercury":
                var uri = command[1];
                var mercury = await connection.Mercury.Get(uri);
                logger.LogInformation(Encoding.UTF8.GetString(mercury.Body.Span));
                break;
            case "-seek":
                if (TimeSpan.TryParse(command[1], out var ts))
                    await connection.Playback.Seek(ts, CancellationToken.None);
                break;
            case "-play":
                var id = SpotifyId.FromUri(command[1]);
                switch (id.Type)
                {
                    case AudioItemType.Track:
                        var playback = await connection.Playback.PlayTrack(
                            command[1],
                            Option<PreferredQualityType>.None,
                            CancellationToken.None);
                        break;
                    case AudioItemType.Playlist:
                    case AudioItemType.Album:
                        var p = await connection.Playback.PlayContext(
                            command[1],
                            0,
                            CancellationToken.None);
                        break;
                }

                break;
            case "-pause":
                await connection.Playback.Pause(CancellationToken.None);
                break;
        }
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error");
    }
}

// //https://open.spotify.com/track/3K8wfMDLxwtLGuVrYobxVe?si=518e3b12e14443fb
// //https://open.spotify.com/playlist/2TAyTkj953qz8fG2IXq1V0?si=1028b95ac678473e
// //https://open.spotify.com/track/0afoCntatBcJGjz525RxBT?si=62bb8201d730434d
// //https://open.spotify.com/track/49jhaFKylisSzgaReEP2Jt?si=4eef71c9b1cf4897
// //var playback = await connection.Playback.PlayContext
// var playback = await connection.Playback.PlayTrack("spotify:track:49jhaFKylisSzgaReEP2Jt",
//     Option<PreferredQualityType>.None,
//     CancellationToken.None);

Console.ReadLine();
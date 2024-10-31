using System.Diagnostics;
using ConsoleApp1.Player;
using ConsoleApp1.PseudoUI;
using Eum.Spotify;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Serilog;
using Wavee;
using Wavee.Config;
using Wavee.Interfaces;
using Wavee.Services.Playback;
using Wavee.ViewModels;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.Service;
using Wavee.ViewModels.State;
using Wavee.ViewModels.ViewModels;
using Wavee.ViewModels.ViewModels.Library;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    // .Enrich.WithProperty("LoggerName", typeof(Program).FullName)
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    //outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3} {LoggerName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Debug(
        outputTemplate:
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
var loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(); });

var dataDir = AppContext.BaseDirectory;
UiConfig uiConfig = LoadOrCreateUiConfig(dataDir);
var configFilePath = Path.Combine(dataDir, "Config.json");
Services.Initialize(
    dataDir,
    configFilePath, new PersistentConfig(), uiConfig, new SingleInstanceChecker(),
    new TerminateService(TerminateApplicationAsync, TerminateApplication));
var uiContext = CreateUiContext();
UiContext.Default = uiContext;

var lifetime = new ActivatableApplicationLifetime();
var applicationStateManager = new ApplicationStateManager(
    new MainWindowFactory(),
    lifetime,
    uiContext,
    false);

void TerminateApplication()
{
    throw new NotImplementedException();
}

Task TerminateApplicationAsync()
{
    throw new NotImplementedException();
}



var config = new SpotifyConfig(new SpotifyCredentialsCache(RetrieveCredentials, StoreCredentials))
{
    Cache = new SpotifyCacheConfig
    {
        Location = "C:\\Users\\ckara\\Desktop\\waveecache"
    }
};
ISpotifyClient client =
    new SpotifyClient(new WaveePlayer(loggerFactory.CreateLogger<IWaveePlayer>()), config, loggerFactory);


// var libraryService = new SpotifyLibraryService(client);
// var libraries = new LibrariesViewModel(libraryService);
// await libraryService.InitializeAsync();

var sw = Stopwatch.StartNew();

client.PlaybackClient.PlaybackState.Subscribe(async x =>
{
    switch (x)
    {
        case SpotifyLocalPlaybackState spotifyLocalPlaybackState:
        {
            break;
        }
        case SpotifyRemotePlaybackState spotifyRemotePlaybackState:
        {
            // var builder = PlayItemCommandBuilderFactory
            //     .FromLikedSongs(userId: await client.UserId())
            //     .Sort(SortDescription.Artist);
            // await client.PlaybackClient.Play(builder);

            // Log: Currently playing: {Name} (Duration)
            if (spotifyRemotePlaybackState.CurrentTrack is not null)
            {
                Log.Information("Currently playing: {Name} ({Position} / {Duration})",
                    spotifyRemotePlaybackState.CurrentTrack.Name,
                    spotifyRemotePlaybackState.Position,
                    spotifyRemotePlaybackState.CurrentTrack.Duration);
            }
            else
            {
                // Log: No track is currently playing.
                Log.Information("No track is currently playing.");
            }

            break;
        }
        case NonePlaybackState spotifyNoPlaybackState:
        {
            break;
        }
        case ErrorPlaybackState spotifyErrorPlaybackState:
        {
            break;
        }
    }
});
await client.PlaybackClient.ConnectToRemoteControl(null, null);

sw.Stop();

Console.ReadLine();


ValueTask StoreCredentials(LoginCredentials credentials, CancellationToken cancellationtoken)
{
    // write to file: credentials.dat
    var data = credentials.ToByteString().ToBase64();
    File.WriteAllText("credentials.dat", data);
    return ValueTask.CompletedTask;
}

ValueTask<LoginCredentials?> RetrieveCredentials(CancellationToken cancellationtoken)
{
    if (File.Exists("credentials.dat"))
    {
        var data = File.ReadAllText("credentials.dat");
        var bytestring = LoginCredentials.Parser.ParseFrom(ByteString.FromBase64(data));
        return new ValueTask<LoginCredentials?>(bytestring);
    }

    return new ValueTask<LoginCredentials>((LoginCredentials?)null);
}

UiContext CreateUiContext()
{
    var applicationSettings = CreateApplicationSettings();

    // This class (App) represents the actual Wavee Application and it's sole presence means we're in the actual runtime context (as opposed to unit tests)
    // Once all ViewModels have been refactored to receive UiContext as a constructor parameter, this static singleton property can be removed.
    return new UiContext(
        applicationSettings,
        new UserRepository());
}

static IApplicationSettings CreateApplicationSettings()
{
    return new ApplicationSettings(
        Services.PersistentConfigFilePath,
        Services.PersistentConfig,
        Services.UiConfig);
}

static UiConfig LoadOrCreateUiConfig(string dataDir)
{
    Directory.CreateDirectory(dataDir);

    UiConfig uiConfig = new(Path.Combine(dataDir, "UiConfig.json"));
    uiConfig.LoadFile(createIfMissing: true);

    return uiConfig;
}
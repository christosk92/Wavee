using Eum.Spotify.connectstate;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Application.Playback;
using Wavee.Domain.Playback.Player;
using Wavee.Players.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Common.Contracts;

var storagePath = "/d/";
Directory.CreateDirectory(storagePath);
var sp = new ServiceCollection()
    .AddSpotify(new SpotifyClientConfig
    {
        Storage = new StorageSettings
        {
            Path = storagePath
        },
        Remote = new SpotifyRemoteConfig
        {
            DeviceName = "Wavee debug",
            DeviceType = DeviceType.Computer
        },
        Playback = new SpotifyPlaybackConfig
        {
            InitialVolume = 0.5
        }
    })
    .WithStoredOrOAuthModule(OpenBrowser)
    .WithPlayer<NAudioPlayer>()
    .AddMediator()
    .BuildServiceProvider();

var client = sp.GetRequiredService<ISpotifyClient>();
const string filePathMp3 = "C:\\Users\\ckara\\Downloads\\4dba53850d6bfdb9800d53d65fe2e5f1369b9040.mp3";
client.Player.Crossfade(TimeSpan.FromSeconds(10));
await client.Player.Play(WaveePlaybackList.Create(
    LocalMediaSource.CreateFromFilePath(filePathMp3),
    LocalMediaSource.CreateFromFilePath(filePathMp3)));

// client.PlaybackStateChanged += (sender, state) =>
// {
//     Console.WriteLine($"Playback state changed: {state}");
// };
// await client.Initialize();
// var x = "";
Console.ReadLine();

static Task<OpenBrowserResult> OpenBrowser(string url, CancellationToken ct)
{
    Console.WriteLine($"Open browser: {url} and return redirect url");

    var redirect = Console.ReadLine();
    return Task.FromResult(new OpenBrowserResult(redirect, true));
}
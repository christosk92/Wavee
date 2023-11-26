using System.Reflection.Metadata;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
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
    .AddMediator()
    .BuildServiceProvider();

var client = sp.GetRequiredService<ISpotifyClient>();
await client.Initialize();
var x = "";
Console.ReadLine();

static Task<OpenBrowserResult> OpenBrowser(string url, CancellationToken ct)
{
    Console.WriteLine($"Open browser: {url} and return redirect url");

    var redirect = Console.ReadLine();
    return Task.FromResult(new OpenBrowserResult(redirect, true));
}
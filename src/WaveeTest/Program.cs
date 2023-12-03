using System.Diagnostics;
using Eum.Spotify.connectstate;
using Eum.Spotify.playplay;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Spotify.Collection.Proto.V2;
using Spotify.PlayedState.Proto;
using Wavee.Application.Playback;
using Wavee.Domain.Playback.Player;
using Wavee.Players.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Application.Playback;
using Wavee.Spotify.Common.Contracts;

var storagePath = "/d/";
const string b =
    "Ci0KJXNwb3RpZnk6YXJ0aXN0OjBLODdmM293ZW16SThOVUNvRUlYT0IQye2sqwYKKAogc3BvdGlmeTpjb2xsZWN0aW9uOnlvdXItZXBpc29kZXMQ58zCkQYKLwonc3BvdGlmeTpwbGF5bGlzdDozN2k5ZFFaRjFEWGFQOTZnd1BSTGVlEOvSwIQGGg0xNzAxNTM0NTcyMDc3";
var playedState = PageResponse.Parser.ParseFrom(ByteString.FromBase64(b));


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
            InitialVolume = 0.5,
            PreferedQuality = SpotifyAudioQuality.High
        }
    })
    .WithStoredOrOAuthModule(OpenBrowser)
    .WithPlayer<NAudioPlayer>()
    .AddMediator()
    .BuildServiceProvider();

var client = sp.GetRequiredService<ISpotifyClient>();
await client.Initialize();

var sw = Stopwatch.StartNew();
//var result = await client.Search.SearchAsync("empire", 0, 2);
var autoCompleteResult = await client.Search.Autocomplete("re", cancellationToken: CancellationToken.None);
sw.Stop();



const string filePathMp3 = "C:\\Users\\ckara\\Downloads\\4dba53850d6bfdb9800d53d65fe2e5f1369b9040.mp3";
client.Player.Crossfade(TimeSpan.FromSeconds(10));
// await client.Player.Play(WaveePlaybackList.Create(
//     LocalMediaSource.CreateFromFilePath(filePathMp3),
//     LocalMediaSource.CreateFromFilePath(filePathMp3)));

//await client.Initialize();

var spotifyTrack = SpotifyMediaSource.CreateFromUri(client, "spotify:track:210JJAa9nJOgNa0YNrsT5g");
await client.Player.Play(WaveePlaybackList.Create(spotifyTrack));

// client.PlaybackStateChanged += (sender, state) =>
// {
//     Console.WriteLine($"Playback state changed: {state}");
// };
// var x = "";
Console.ReadLine();

static Task<OpenBrowserResult> OpenBrowser(string url, CancellationToken ct)
{
    Console.WriteLine($"Open browser: {url} and return redirect url");

    var redirect = Console.ReadLine();
    return Task.FromResult(new OpenBrowserResult(redirect, true));
}
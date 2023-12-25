using System.Diagnostics;
using ConsoleApp1;
using LiteDB;
using Wavee;
using Wavee.Core.Enums;
using Wavee.Core.Playback;
using Wavee.Players.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.Core.Models.Common;

var db = new LiteDatabase("credentials.db");
var repo = new LiteDbCredentialsStorage(db);
var config = new WaveeSpotifyConfig
{
    CredentialsStorage = new WaveeSpotifyCredentialsStorageConfig
    {
        GetDefaultUsername = () => repo.GetDefaultUserName(),
        OpenCredentials = (name,
            type) =>
        {
            var credentials = repo.GetFor(name,
                type);
            return credentials;
        },
        SaveCredentials = (name,
            type,
            data) =>
        {
            repo.Store(name,
                type,
                data);
        }
    },
    CachingProvider = NullCachingService.Instance
};
var player = new NAudioPlayer();
player.PlaybackStateChanged += (sender, args) => { Console.WriteLine($"Playback state changed to {args}"); };
player.PlaybackError += (sender, args) => { Console.WriteLine($"Playback error: {args.Message}"); };
var client = WaveeSpotifyClient.Create(player, config, OpenBrowserAndReturnCallback);
var ids = new[]
{
    SpotifyId.FromBase62("6WeCNrVIIGnmWH9LX5NpeH", AudioItemType.Track),
    SpotifyId.FromBase62("3HltbvkfjVeyjESx3JbuLD", AudioItemType.Track)
};

var list = WaveePlaybackList.Create(async i => await client.Playback.CreateStream(ids[i], CancellationToken.None),
    ids.Length);
await player.Play(list);
var newConnectionMade = await client.Remote.Connect();
if (newConnectionMade)
{
    Console.WriteLine("Connected!");
}
else
{
    Console.WriteLine("Already connected!");
}

var test = "";

Task<string> OpenBrowserAndReturnCallback(string url)
{
    // Console.WriteLine($"Please open this url in your browser: {url}");
    // Console.WriteLine("Please enter the callback url:");
    // return Task.FromResult(Console.ReadLine());

    Console.WriteLine($"Please open this url in your browser: {url}");

    Console.WriteLine("Please enter the callback url:");
    var callbackUrl = Console.ReadLine();
    return Task.FromResult(callbackUrl);
}
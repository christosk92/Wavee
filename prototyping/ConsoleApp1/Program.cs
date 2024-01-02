using System.Diagnostics;
using ConsoleApp1;
using LiteDB;
using Wavee;
using Wavee.Core.Enums;
using Wavee.Core.Playback;
using Wavee.Players.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Playback;

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
var topTrackContext = SpotifyContextBuilder.New()
    .FromArtist(SpotifyId.FromUri("spotify:artist:3nlHsNqwCSvT988ZfSW1Yh"))
    .FromTopTracks()
    .StartFromIndex(1)
    .Build(client);
var albumContext = SpotifyContextBuilder.New()
    .FromArtist(SpotifyId.FromUri("spotify:artist:28Ky95tmlHktB96DBUoB0g"))
    .FromAlbum(SpotifyId.FromUri("spotify:album:2SxoeF005n621Jca66RRdu"))
    .StartFromIndex(0)
    .Build(client);

//await player.Play(albumContext);
client.Remote.State.Subscribe(x =>
{
    
});
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
using System.Diagnostics;
using System.Text;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee;
using Wavee.Spotify;
using Wavee.Spotify.Helpers.Extensions;
using Wavee.Spotify.Models.Response.Artist;
using Wavee.Spotify.Remote;
using Wavee.Spotify.Remote.Infrastructure.Live;


var credentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};
var spotifyClient = SpotifyClientBuilder.New().Build();

spotifyClient.ConnectionStateChanged.Subscribe(j => { Console.WriteLine($"Connection state changed to {j}"); });
spotifyClient.CountryCodeChanged.Subscribe(j => { Console.WriteLine($"Country code changed to {j}"); });
spotifyClient.ProductInfoChanged.Subscribe(j =>
{
    if (j.IsSome)
    {
        var dict = j.ValueUnsafe();
        foreach (var (key, value) in dict)
        {
            Console.WriteLine($"{key} = {value}");
        }
    }
});

await spotifyClient.Connect(credentials);
var player = new WaveePlayer();
var remote = new LiveSpotifyRemote(spotifyClient, player,
    new SpotifyPlaybackConfig("Wavee",
        DeviceType.Computer, PreferredQualityType.High, ushort.MaxValue / 2));
await remote.Connect();
while (true)
{
    GC.Collect();
    //hm://artist/v1/2dd5mrQZvg6SmahdgVKDzh/desktop?format=json&catalogue=premium&langauge=kr
    var uri = Console.ReadLine();
    var sw2 = Stopwatch.StartNew();
    var k = await spotifyClient.Mercury.Get(uri);
    sw2.Stop();

    var artist = k.DeserializeFromJson<MercuryArtist>()
        .IfNone(() => throw new Exception("Failed to deserialize artist"));

    Console.WriteLine($"Mercury request took {sw2.ElapsedMilliseconds}ms");
}

Console.ReadLine();
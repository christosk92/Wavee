using System.Diagnostics;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Spotify;
using Wavee.Spotify.Artist;
using Wavee.Spotify.Common;

var config = new SpotifyConfig(
    remote: new SpotifyRemoteConfig(
        deviceName: "Wavee",
        deviceType: DeviceType.Computer
    ),
    playback: new SpotifyPlaybackConfig(
        crossfadeDuration: TimeSpan.FromSeconds(10),
        preferedQuality: PreferredQualityType.High
    ),
    cache: new SpotifyCacheConfig(
        cacheRoot: null
    )
);

var client = SpotifyClient.Create(new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass,
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD"))
}, config);
var observable = client.Remote.Updates.Subscribe(x =>
{
    Console.WriteLine(x);
});

await client.Remote.Pause();
var dummyId = SpotifyId.FromUri("spotify:artist:1uNFoZAHBGtllmzznpCI3s");
var sw2 = Stopwatch.StartNew();
var artist = await client.Artist.GetArtistAsync(dummyId, cancellationToken: CancellationToken.None);
var albums = artist.Discography.First();


sw2.Stop();
GC.Collect();
var test = "";
Console.ReadLine();
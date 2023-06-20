using System.Diagnostics;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify;
using Wavee.Spotify.Artist;
using Wavee.Spotify.Common;

var client = SpotifyClient.Create(new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass,
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD"))
});
var dummyId = SpotifyId.FromUri("spotify:artist:1uNFoZAHBGtllmzznpCI3s");
var sw2 = Stopwatch.StartNew();
var artist2 = await client.Artist.GetArtistAsync(dummyId, cancellationToken: CancellationToken.None);

// var hasNextAlbums = artist2.Discography.First().Items.Select((c, i) => (c, i))
//     .SkipWhile(x => x.c.Initialized)
//     .First().i;
//
// var nextPage =
//     await client.Artist.GetDiscographyAsync(dummyId, DiscographyType.Albums, hasNextAlbums, 50,
//         new CancellationToken());

sw2.Stop();
GC.Collect();
var test = "";
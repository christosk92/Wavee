using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify;
using Wavee.Spotify.Common;

var client = SpotifyClient.Create(new LoginCredentials
{
    Typ = AuthenticationType.AuthenticationUserPass, 
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD"))
});
var dummyId = SpotifyId.FromUri("spotify:artist:6RHTUrRF63xao58xh9FXYJ");
var artist = await client.Artist.GetArtistAsync(dummyId, cancellationToken: CancellationToken.None);
var test = "";
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Player;
using Wavee.Spotify;

WaveePlayer.Instance.StateUpdates.Subscribe(state =>
{
    Console.WriteLine(state);
});

var c = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    remote: new SpotifyRemoteConfig(deviceName: "Wavee", deviceType: DeviceType.Computer),
    playback: new SpotifyPlaybackConfig()
);

var spotifyClient = await SpotifyClient.CreateAsync(config, c);
spotifyClient.Remote.StateUpdates.Subscribe(state =>
{
    if (state.IsSome)
    {
        Console.WriteLine(state);
    }
});

const string ctxUri = "spotify:artist:41X1TR6hrK8Q2ZCpp2EqCz";
const int ctxIndex = 0;
await spotifyClient.Playback.Play(ctxUri, ctxIndex);
Console.ReadLine();
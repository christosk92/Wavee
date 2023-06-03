using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Wavee.Player;
using Wavee.Spotify;

WaveePlayer.Instance.StateUpdates.Subscribe(state => { Console.WriteLine(state); });
//
// var ctx = new WaveeContext(
//     Id: "local",
//     Name: "local",
//     FutureTracks: BuildTestTracks(),
//     ShuffleProvider: Option<IShuffleProvider>.None
// );
//
// IEnumerable<FutureWaveeTrack> BuildTestTracks()
// {
//     //"C:\Users\ckara\Music\ifeelyou.mp3"
//     //"C:\Users\chris-pc\Music\goodbyemylove.mp3"
//     //"C:\Users\ckara\Music\OVAN (오반) SHAUN (숀) - She is [128 kbps].mp3"
//     //"C:\Users\chris-pc\Music\goodbyemylove.mp3"
//
//     var track1 = "C:\\Users\\chris-pc\\Music\\sheis.mp3";
//     var track2 = "C:\\Users\\chris-pc\\Music\\ifeelyou.mp3";
//     var track3 = "C:\\Users\\chris-pc\\Music\\goodbyemylove.mp3";
//
//     static WaveeTrack OpenLocalStream(string p0, AudioId id)
//     {
//         var tg = TagLib.File.Create(p0);
//         var duration = tg.Properties.Duration;
//         tg.Dispose();
//         var tream = File.OpenRead(p0);
//
//         return new WaveeTrack(
//             audioStream: tream,
//             title: "track",
//             id: id,
//             metadata: HashMap<string, string>.Empty,
//             duration: duration
//         );
//     }
//
//     return new[]
//     {
//         new FutureWaveeTrack(
//             TrackId: AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr"),
//             TrackUid: "abc",
//             Factory: (_) =>
//                 Task.FromResult(OpenLocalStream(track1, AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr")))),
//
//         new FutureWaveeTrack(
//             TrackId: AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr"),
//             TrackUid: "def",
//             Factory: (_) =>
//                 Task.FromResult(OpenLocalStream(track2, AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr")))),
//
//         new FutureWaveeTrack(
//             TrackId: AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr"),
//             TrackUid: "ghi",
//             Factory: (_) =>
//                 Task.FromResult(OpenLocalStream(track3, AudioId.FromUri("spotify:track:7n2FZQsaLb7ZRfRPfEeIvr")))),
//     };
// }
//
// NAudioSink.Instance.Resume();
// WaveePlayer.Instance.CrossfadeDuration = TimeSpan.FromSeconds(10);
// await WaveePlayer.Instance.Play(ctx, 0, Option<TimeSpan>.None, false, CancellationToken.None);
// while (true)
// {
//     //commands:
//     //-seek 00:01:00
//     //-resume
//     //-pause
//     //-next
//     //-info
//
//     var cmd = Console.ReadLine();
//     var cmdParts = cmd.Split(' ');
//     var cmdName = cmdParts[0];
//     switch (cmdName)
//     {
//         case "-info":
//         {
//             var state = WaveePlayer.Instance.State;
//             Console.WriteLine(state);
//             break;
//         }
//         case "-duration":
//         {
//             var state = WaveePlayer.Instance.State;
//             Console.WriteLine(state.TrackDetails.ValueUnsafe().Duration);
//             break;
//         }
//         case "-seek":
//         {
//             var seekTo = TimeSpan.Parse(cmdParts[1]);
//             WaveePlayer.Instance.SeekTo(seekTo);
//             break;
//         }
//         case "-resume":
//         {
//             WaveePlayer.Instance.Resume();
//             break;
//         }
//         case "-pause":
//         {
//             WaveePlayer.Instance.Pause();
//             break;
//         }
//     }
// }
//
var c = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    remote: new SpotifyRemoteConfig(deviceName: "Wavee", deviceType: DeviceType.Computer),
    playback: new SpotifyPlaybackConfig(
        crossfadeDuration: TimeSpan.FromSeconds(10),
        preferedQuality: PreferredQualityType.VeryHigh),
    cache: new SpotifyCacheConfig(cacheRoot: Option<string>.None)
);

var spotifyClient = await SpotifyClient.CreateAsync(config, c);

spotifyClient.Remote.StateUpdates.Subscribe(state =>
{
    if (state.IsSome)
    {
        Console.WriteLine(state);
    }
});
//https://open.spotify.com/track/26hOm7dTtBi0TdpDGl141t?si=63327ca800964207
const string ctxUri = "spotify:artist:6HvZYsbFfjnjFrWF950C9d";
const int ctxIndex = 0;
//await spotifyClient.Remote.Takeover();
await spotifyClient.Playback.Play(ctxUri, 3, Option<TimeSpan>.None);
Console.ReadLine();
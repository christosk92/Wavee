using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Spotify.Metadata;
using Wavee;
using Wavee.AudioOutput.LibVLC;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Live;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Player;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Infrastructure;

LibVlcOutput.SetAsMainOutput();

//"C:\Users\chris-pc\Music\goodbyemylove.mp3"
// using var fs = File.OpenRead(@"C:\Users\chris-pc\Music\goodbyemylove.mp3");
// var waiter = AudioOutput<WaveeRuntime>.PlayStream(fs, span =>
//     {
//         var ts = span;
//     }, true)
//     .Run(WaveeCore.Runtime).ThrowIfFail();
// await waiter;

var trackId = new AudioId("goodbyemylove", AudioItemType.Track, "local");

WaveePlayer.StateChanged.Subscribe(state =>
{
    Console.WriteLine(state);
});

WaveePlayer.PlayContext(new WaveeContext(
    Option<IShuffleProvider>.None,
    new AudioId("local", AudioItemType.Unknown, "local"),
    "Local",
    new List<FutureTrack>
    {
        new FutureTrack(trackId, StreamFuture)
    }
), TimeSpan.Zero, 0, false);

Task<IAudioStream> StreamFuture()
{
    var fs = File.OpenRead(@"C:\Users\chris-pc\Music\goodbyemylove.mp3");
    return Task.FromResult<IAudioStream>(new MaskedFsStream(fs, trackId));
}

var c2 = Console.ReadLine();
var credentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    CachePath: Option<string>.None,
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee Test",
        DeviceType.Computer
    ),
    Playback: new SpotifyPlaybackConfig(
        PreferredQualityType.High,
        Autoplay: true
    )
);


var spotifyCore = await SpotifyClient.Create(credentials, config, Option<ILogger>.None);
spotifyCore.RemoteClient.StateChanged.Subscribe(x => { Console.WriteLine(x); });
await spotifyCore.PlaybackClient.PlayContext("spotify:playlist:37i9dQZF1E8LX1dPtJZHnT", 0, TimeSpan.Zero, true,
    CancellationToken.None);

var track = await spotifyCore.GetTrackAsync("3HGlpIqHEInelCfDZYd0Ki");
var c = Console.ReadLine();

class MaskedFsStream : IAudioStream
{
    private readonly FileStream _fs;

    public MaskedFsStream(FileStream fs, AudioId id)
    {
        _fs = fs;
        Track = new DummyTrack(id, "test", Seq<ITrackArtist>.Empty, null, TimeSpan.MaxValue, true);
    }

    public ITrack Track { get; }

    public Stream AsStream()
    {
        return _fs;
    }

    private readonly record struct DummyTrack(AudioId Id, string Title, Seq<ITrackArtist> Artists, ITrackAlbum Album, TimeSpan Duration, bool CanPlay) : ITrack;
}
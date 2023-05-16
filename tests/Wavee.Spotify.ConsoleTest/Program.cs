using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Wavee.AudioOutput.LibVLC;
using Wavee.AudioOutput.NAudio;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Infrastructure;

NAudioOutput.SetAsMainOutput();

var credentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    CachePath: "cache_temp.db",
    Remote: new SpotifyRemoteConfig(
        DeviceName: "Wavee Test",
        DeviceType.Computer
    ),
    Playback: new SpotifyPlaybackConfig(
        PreferredQualityType.Highest,
        Autoplay: true
    )
);

//https://open.spotify.com/playlist/2I0GZgd0hm7A3OO4iZjTN4?si=6f0520640fee4499
//https://open.spotify.com/playlist/1xaI099prElMKypSdl40Bl?si=f3e7559f3ed7421d
//https://open.spotify.com/album/6lumjI581TEGHeTviSikrm?si=cc17ba62e1c34c46
//https://open.spotify.com/playlist/37i9dQZF1DZ06evO2y645c?si=d1aa4d81c0644769
var spotifyCore = await SpotifyClient.Create(credentials, config, Option<ILogger>.None);
spotifyCore.RemoteClient.StateChanged.Subscribe(x => { Console.WriteLine(x); });
await spotifyCore.PlaybackClient.PlayContext("spotify:album:67ExLGAc8JcGet2d4CgJKg",
    0, TimeSpan.Zero, true,
    Option<PreferredQualityType>.None,
    CancellationToken.None);

var c = Console.ReadLine();

class MaskedFsStream : IAudioStream
{
    private readonly FileStream _fs;

    public MaskedFsStream(FileStream fs, AudioId id)
    {
        _fs = fs;
        Track = new DummyTrack(id, "test", Seq<ITrackArtist>.Empty, null, TimeSpan.MaxValue, true);
        CrossfadeController = new CrossfadeController(TimeSpan.FromSeconds(10));
    }

    public ITrack Track { get; }
    public Option<string> Uid { get; }

    public Stream AsStream()
    {
        return _fs;
    }

    public Option<CrossfadeController> CrossfadeController { get; }

    private readonly record struct DummyTrack(AudioId Id, string Title, Seq<ITrackArtist> Artists, ITrackAlbum Album,
        TimeSpan Duration, bool CanPlay) : ITrack;
}
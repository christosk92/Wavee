using System.Diagnostics;
using System.Runtime.InteropServices;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using NAudio.Vorbis;
using Wavee.Core.Ids;
using Wavee.Player;
using Wavee.Sinks;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Playback;

WaveePlayer.Instance.StateUpdates.Subscribe(state => { Console.WriteLine(state); });

var c = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var config = new SpotifyConfig(
    remote: new SpotifyRemoteConfig(deviceName: "Wavee", deviceType: DeviceType.Computer),
    playback: new SpotifyPlaybackConfig(),
    cache: new SpotifyCacheConfig(cacheRoot: Option<string>.None)
);

var spotifyClient = await SpotifyClient.CreateAsync(config, c);

if (spotifyClient.Playback is SpotifyPlaybackClient pc)
{
    //https://open.spotify.com/track/0Oq4WhNwjjLcntxZl4zzr0?si=1c2aa4698713438e
    var trackUri = "spotify:track:0Oq4WhNwjjLcntxZl4zzr0";
    var trackId = AudioId.FromUri(trackUri);
    var mercury = spotifyClient.Mercury;
    var audioKeyProvider = spotifyClient.AudioKeyProvder;
    var cache = spotifyClient.Cache;
    var sw = Stopwatch.StartNew();
    var result = await pc.StreamTrackSpecifically(trackId, HashMap<string, string>.Empty, "US", mercury,
        audioKeyProvider, cache,
        CancellationToken.None);
    var normData = NormalisationData.ParseFromOgg(result.AudioStream)
        .IfNone(new NormalisationData());
    
    var decoder = new VorbisWaveReader(result.AudioStream);
    NAudioSink.Instance.Resume();

    void DoLoop()
    {
        const int readSamples = 4096;
        var buffer = new float[readSamples];
        var read = 0;
        while ((read = decoder.Read(buffer, 0, readSamples)) > 0)
        {
            //do something with the buffer
            //from float to byte[]
            Span<byte> bytes = MemoryMarshal.Cast<float, byte>(buffer);
            NAudioSink.Instance.Write(bytes);
        }
    }

    DoLoop();
    sw.Stop();
}


// spotifyClient.Remote.StateUpdates.Subscribe(state =>
// {
//     if (state.IsSome)
//     {
//         Console.WriteLine(state);
//     }
// });
//
// const string ctxUri = "spotify:artist:41X1TR6hrK8Q2ZCpp2EqCz";
// const int ctxIndex = 0;
// await spotifyClient.Remote.Takeover();
//await spotifyClient.Playback.Play(ctxUri, ctxIndex);
Console.ReadLine();
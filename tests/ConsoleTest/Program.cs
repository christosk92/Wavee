using System.Diagnostics;
using System.Text;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Spotify;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Playback;
using static LanguageExt.Prelude;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};
//https://open.spotify.com/track/786ymAh5BmHoIpvjyrvjXk?si=3c109608329441ce
//https://open.spotify.com/track/2CgOd0Lj5MuvOqzqdaAXtS?si=32ac013b22c4435f
//https://open.spotify.com/track/4ewazQLXFTDC8XvCbhvtXs?si=52de2819ac6d47fd
//https://open.spotify.com/track/0mf82mK5aeZm4vN9HM2InQ?si=df4d118bb389440f
var trackId = new SpotifyId("spotify:track:786ymAh5BmHoIpvjyrvjXk");

var spotifyConfig = new SpotifyConfig(DeviceName: "Wavee",
    DeviceType.Computer);
var client = await SpotifyRuntime.Authenticate(spotifyConfig, None, loginCredentials);

var playbackConfig = new SpotifyPlaybackConfig(
    PreferredQualityType.High,
    ushort.MaxValue / 2
);
// var playbackStream = await client.StreamAudio(trackId, playbackConfig);
// var decoder = new VorbisWaveReader(playbackStream.AsStream());
// var waveOut = new WaveOutEvent();
// waveOut.Init(decoder);
// waveOut.Play();
//var searchTest = await client.Mercury.Search("jukjae", "artist");

while (true)
{
    var msg = Console.ReadLine();
    var sw = Stopwatch.StartNew();

    //format is [GET|SEND|] uri
    MercuryMethod method;
    if (msg.StartsWith("get "))
    {
        method = MercuryMethod.Get;
        msg = msg.Substring(4);
    }
    else if (msg.StartsWith("send "))
    {
        method = MercuryMethod.Send;
        msg = msg.Substring(5);
    }
    else
    {
        method = MercuryMethod.Get;
    }

    var test = await client.Mercury.Send(
        method,
        msg,
        None);
    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"{test.Header.StatusCode}");
    Console.WriteLine(Encoding.UTF8.GetString(test.Body.Span));
    GC.Collect();
}
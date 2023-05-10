using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee;
using Wavee.Infrastructure.Live;
using Wavee.Player;
using Wavee.Player.Playback;
using Wavee.Spotify;
using Wavee.Spotify.Playback;
using Wavee.Spotify.Playback.Sys;
using Wavee.Spotify.Remote;
using Wavee.Spotify.Sys.AudioKey;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Metadata;
using Wavee.Spotify.Sys.Tokens;
using static LanguageExt.Prelude;

// Option<IWaveePlayerState> State = Option<IWaveePlayerState>.None;
// IDisposable listener = default;
// while (true)
// {
//     //commands:
//     //play (source)
//     //pause
//     //seek (time)
//     //info 
//
//     //set encoding to UTF8
//     Console.OutputEncoding = System.Text.Encoding.Unicode;
//     Console.InputEncoding = System.Text.Encoding.Unicode;
//     var command = Console.ReadLine();
//     //path can contain spaces, so we split by space and take the first part as the command name
//     var commandName = command.Split(' ')[0];
//     //the rest of the command is the arguments
//     var commandArgs = command.Split(' ')[1..];
//     
//     switch (commandName)
//     {
//         case "play":
//         {
//             listener?.Dispose();
//             var source = string.Join(' ', commandArgs);
//             //remove quotes
//             if (source.StartsWith("\"") && source.EndsWith("\""))
//             {
//                 //"C:\Users\chris-pc\Music\Jang Beom June - 고백 (Go Back) (1).mp3"
//                 // -> C:\Users\chris-pc\Music\Jang Beom June - 고백 (Go Back) (1).mp3
//                 source = source[1..^1];
//             }
//             var fs = new FileMaskedAsAudioStream(File.Open(source, FileMode.Open), default);
//             listener = await WaveePlayer.Play(fs, state =>
//             {
//                 State = Option<IWaveePlayerState>.Some(state);
//                 Console.WriteLine(state);
//             });
//             break;
//         }
//         case "resume":
//             if (State.IsNone)
//             {
//                 Console.WriteLine("resume requires a valid state");
//                 break;
//             }
//             await WaveePlayer.Resume();
//             break;
//         case "pause":
//             if (State.IsNone)
//             {
//                 Console.WriteLine("pause requires a valid state");
//                 break;
//             }
//             await WaveePlayer.Pause();
//             break;
//         case "seek":
//             if (commandArgs.Length != 1)
//             {
//                 Console.WriteLine("seek requires exactly one argument");
//                 break;
//             }
//             if (!TimeSpan.TryParse(commandArgs[0], out var time))
//             {
//                 Console.WriteLine("seek requires a valid TimeSpan argument");
//                 break;
//             }
//             if (State.IsNone)
//             {
//                 Console.WriteLine("seek requires a valid state");
//                 break;
//             }
//             await WaveePlayer.Seek(time);
//             break;
//         case "info":
//             if (State.IsSome)
//             {
//                 Console.WriteLine(State.ValueUnsafe());
//             }
//             else
//             {
//                 Console.WriteLine("No state");
//             }
//             break;
//     }
// }
//
// Console.ReadLine();

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var info = await SpotifyClient.Authenticate(loginCredentials);

var countryCode = await info.CountryCode();
var productInfo = await info.ProductInfo();

var remoteInfo = await SpotifyRemoteClient.ConnectRemote(
    info,
    config: new SpotifyPlaybackConfig(
        DeviceName: "Wavee",
        DeviceType.Computer,
        InitialVolume: 0.5f,
        PreferredQualityType.High
    ));
remoteInfo.ClusterChanged.Subscribe((c) =>
{
    GC.Collect();
    Console.WriteLine("Cluster changed");
});
remoteInfo.CollectionChanged.Subscribe((items) =>
{
    Console.WriteLine("Collection changed");
    items.Iter((item) =>
    {
        Console.WriteLine(item);
    });
});
Console.ReadLine();

class FileMaskedAsAudioStream : IAudioStream
{
    private readonly FileStream _fileStream;

    public FileMaskedAsAudioStream(FileStream fileStream, IPlaybackItem item)
    {
        _fileStream = fileStream;
        Item = item;
    }

    public string ItemId => _fileStream.Name;
    public IPlaybackItem Item { get; }
    public Stream AsStream() => _fileStream;
}
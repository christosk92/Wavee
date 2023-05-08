using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee;
using Wavee.Player;
using Wavee.Spotify;

Option<IWaveePlayerState> State = Option<IWaveePlayerState>.None;
IDisposable listener = default;
while (true)
{
    //commands:
    //play (source)
    //pause
    //seek (time)
    //info 

    //set encoding to UTF8
    Console.OutputEncoding = System.Text.Encoding.Unicode;
    Console.InputEncoding = System.Text.Encoding.Unicode;
    var command = Console.ReadLine();
    //path can contain spaces, so we split by space and take the first part as the command name
    var commandName = command.Split(' ')[0];
    //the rest of the command is the arguments
    var commandArgs = command.Split(' ')[1..];
    
    switch (commandName)
    {
        case "play":
        {
            listener?.Dispose();
            var source = string.Join(' ', commandArgs);
            //remove quotes
            if (source.StartsWith("\"") && source.EndsWith("\""))
            {
                //"C:\Users\chris-pc\Music\Jang Beom June - 고백 (Go Back) (1).mp3"
                // -> C:\Users\chris-pc\Music\Jang Beom June - 고백 (Go Back) (1).mp3
                source = source[1..^1];
            }
            var fs = new FileMaskedAsAudioStream(File.Open(source, FileMode.Open), default);
            listener = await WaveePlayer.Play(fs, state =>
            {
                State = Option<IWaveePlayerState>.Some(state);
                Console.WriteLine(state);
            });
            break;
        }
        case "resume":
            if (State.IsNone)
            {
                Console.WriteLine("resume requires a valid state");
                break;
            }
            await WaveePlayer.Resume();
            break;
        case "pause":
            if (State.IsNone)
            {
                Console.WriteLine("pause requires a valid state");
                break;
            }
            await WaveePlayer.Pause();
            break;
        case "seek":
            if (commandArgs.Length != 1)
            {
                Console.WriteLine("seek requires exactly one argument");
                break;
            }
            if (!TimeSpan.TryParse(commandArgs[0], out var time))
            {
                Console.WriteLine("seek requires a valid TimeSpan argument");
                break;
            }
            if (State.IsNone)
            {
                Console.WriteLine("seek requires a valid state");
                break;
            }
            await WaveePlayer.Seek(time);
            break;
        case "info":
            if (State.IsSome)
            {
                Console.WriteLine(State.ValueUnsafe());
            }
            else
            {
                Console.WriteLine("No state");
            }
            break;
    }
}

Console.ReadLine();

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var info = await SpotifyClient.Authenticate(loginCredentials);
var countryCode = await info.CountryCode();
var productInfo = await info.ProductInfo();
var countryCodeAgain = await info.CountryCode();


// var remoteInfo = await info.Connect(
//     player: player,
//     config: new SpotifyRemoteConfig(
//         DeviceName: "Wavee",
//         DeviceType.Computer,
//         PreferredQualityType.High
//     ));
// remoteInfo.ClusterChanged.Subscribe((c) =>
// {
//     var k = c.ValueUnsafe();
// });
Console.ReadLine();

class FileMaskedAsAudioStream : IAudioStream
{
    private readonly FileStream _fileStream;

    public FileMaskedAsAudioStream(FileStream fileStream, IPlayingItem item)
    {
        _fileStream = fileStream;
        Item = item;
    }

    public IPlayingItem Item { get; }
    public Stream AsStream() => _fileStream;
}
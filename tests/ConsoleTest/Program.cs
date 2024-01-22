using Eum.Spotify.connectstate;
using Google.Protobuf;
using Konsole;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using Serilog;
using Wavee;
using Wavee.Spfy;
using Wavee.Spfy.Remote;
using Wavee.Spotify.Utils;

Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();
//
// var base64gzipp =
//     "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";
// var gzipdecompress = Gzip.UnsafeDecompressAltAsMemory(Convert.FromBase64String(base64gzipp));
// var a = PutStateRequest.Parser.ParseFrom(ByteString.FromBase64(Convert.ToBase64String(gzipdecompress.Span)));

var player = new WaveePlayer(Serilog.Log.Logger);
// var mp3s = Directory.GetFiles("C:\\Users\\chris\\Music\\Chillstep\\VOL1", "*.mp3", SearchOption.AllDirectories);
// var wave32s = mp3s
//     .OrderBy(x =>
//     {
//         using var tag = TagLib.File.Create(x);
//         return tag.Tag.Track;
//     })
//     .Select(x =>
//     {
//         var y = () =>
//         {
//             var z = new WaveChannel32(new Mp3FileReader(x));
//             return new ValueTask<WaveeStream>(new WaveeStream(z, LocalFile.FromPath(x)));
//         };
//         return y;
//     }).ToArray();

player.TrackChanged += (sender, track) => { Serilog.Log.Information("Track changed: {Track}", track); };
player.PositionChanged += (sender, state) =>
{
    if (state.Reason is WaveePlayerPositionChangedEventType.UserRequestedSeeked)
    {
        Serilog.Log.Information("Position changed: {Position}", state.Position);
    }
};
player.PausedChanged += (sender, paused) => { Serilog.Log.Information("Paused changed: {Paused}", paused); };

//await player.Play(Option<TimeSpan>.Some(TimeSpan.FromSeconds(10)), wave32s);

// new Thread((async _ =>
// {
//     var bar = new ProgressBar(1);
//
//     while (true)
//     {
//         var time = player.Position;
//         if (time.IsSome)
//         {
//             bar.Max = (int)player.Duration.ValueUnsafe().TotalSeconds;
//             bar.Refresh((int)time.ValueUnsafe().TotalSeconds,
//                 $@"[{time.ValueUnsafe():mm\:ss}/{player.Duration.ValueUnsafe():mm\:ss}]");
//             //   pb1.Report(time.ValueUnsafe().TotalSeconds / player.Duration.ValueUnsafe().TotalSeconds);
//         }
//
//
//         // wait for 1/10th of a second
//         await Task.Delay(100);
//     }
// })).Start();
// while (true)
// {
//     // command: -seek -f {fraction}
//     // command: -seek -t {timestamp}
//     // command: pause 
//     var command = Console.ReadLine();
//     if (command == "exit")
//     {
//         break;
//     }
//
//     if (command.StartsWith("-seek"))
//     {
//         var parts = command.Split(' ');
//         if (parts.Length != 3)
//         {
//             Console.WriteLine("Invalid seek command");
//             continue;
//         }
//
//         var seekType = parts[1];
//         var seekValue = parts[2];
//         if (seekType == "-f")
//         {
//             if (!double.TryParse(seekValue, out var fraction))
//             {
//                 Console.WriteLine("Invalid seek fraction");
//                 continue;
//             }
//
//             player.SeekAsFraction(fraction);
//         }
//         else if (seekType == "-t")
//         {
//             if (!TimeSpan.TryParse(seekValue, out var timestamp))
//             {
//                 Console.WriteLine("Invalid seek timestamp");
//                 continue;
//             }
//             //TODO
//         }
//         else
//         {
//             Console.WriteLine("Invalid seek type");
//             continue;
//         }
//     }
//     else if (command == "pause")
//     {
//         if (player.Paused)
//         {
//             player.Resume();
//         }
//         else
//         {
//             player.Pause();
//         }
//     }
//     else
//     {
//         Console.WriteLine("Invalid command");
//     }
// }

var microsoftLogging = new LoggerFactory()
    .AddSerilog();
var appLogger = microsoftLogging.CreateLogger("ConsoleApp");

var client = new WaveeSpotifyClient(OpenBrowser, new MemorySecureStorage(),
    microsoftLogging.CreateLogger("WaveeSpotifyClient"),
    player);

client.Remote.StateChanged += (sender, state) => { appLogger.LogInformation("Remote state changed: {State}", state); };
client.Remote.StatusChanged += (sender, status) =>
{
    appLogger.LogInformation("Remote status changed: {Wsstatus}", status);
    if (status == RemoteConnectionStatusType.NotConnectedDueToError)
    {
        appLogger.LogError("Error: {WsLastError}", client.LastError);
    }
};
client.TcpConnectionStatusChanged += (sender, status) =>
{
    appLogger.LogInformation("Tcp status changed: {Tcpstatus}", status);
    if (status == TcpConnectionStatusType.NotConnectedDueToError)
    {
        appLogger.LogError("Error: {TcpLastError}", client.LastError);
    }
};

_ = client.CountryCode.AsTask().ContinueWith(x =>
{
    appLogger.LogInformation("CountryCode: {CountryCode}", x.Result);
});
await client.Authenticate();
//https://open.spotify.com/track/2zDIxlHcudE8yaB88wQAoP?si=b5defc41768e4a71
//await client.Playback.Play(SpotifyId.FromUri("spotify:track:1z4mivQugjaobIZAqR4N4U"), Option<int>.None);
await client.Playback.Play(SpotifyId.FromUri("spotify:playlist:1xaI099prElMKypSdl40Bl"), Option<int>.Some(0));
//await client.Playback.Play(SpotifyId.FromUri("spotify:album:56XzxNKUGySZcu1nByxo3y"), Option<int>.Some(1));


var testmp3 = "C:\\Users\\chris\\Music\\Tristam Questions.mp3";
var uri = SpotifyId.FromUri("spotify:track:6wv7nGvhdFLZD4XmFEt33C");
await client.Metadata.PopulateLocalTrackWithSpotifyMetadata(testmp3, uri);


ValueTask<string> OpenBrowser(string url, Func<string, bool> shouldreturn)
{
    Console.WriteLine("Please open the following url in your browser:");
    Console.WriteLine(url);
    Console.WriteLine("Result:");

    while (true)
    {
        var result = Console.ReadLine();
        var shouldReturn = shouldreturn(result);
        if (!shouldReturn)
        {
            Console.WriteLine("Invalid input. Please try again.");
        }
        else
        {
            return new ValueTask<string>(result);
        }
    }
}


Console.ReadKey();

public sealed class MemorySecureStorage : ISecureStorage
{
    public ValueTask Remove(string username)
    {
        if (File.Exists($"{username}.txt"))
        {
            File.Delete($"{username}.txt");
        }

        return default;
    }

    public ValueTask Store(string username, string pwd)
    {
        //line seperated
        //username
        //password
        File.WriteAllText($"{username}.txt", pwd);
        File.WriteAllText("default.txt", username);
        return default;
    }

    public bool TryGetDefaultUser(out string userId)
    {
        if (File.Exists("default.txt"))
        {
            userId = File.ReadAllText("default.txt");
            if (File.Exists($"{userId}.txt"))
            {
                return true;
            }
        }

        userId = default;
        return false;
    }

    public bool TryGetStoredCredentialsForUser(string userId, out string password)
    {
        if (File.Exists($"{userId}.txt"))
        {
            password = File.ReadAllText($"{userId}.txt");
            return true;
        }

        password = default;
        return false;
    }
}
// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Wavee.Interfaces.Models;
using Wavee.Playback;
using Wavee.Playback.Converters;
using Wavee.Playback.Volume;
using Wavee.Sinks.NAudio;
using ILogger = Serilog.ILogger;

Console.OutputEncoding = System.Text.Encoding.Unicode;
Console.InputEncoding = System.Text.Encoding.Unicode;

ILogger logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

var loggerFactory = new LoggerFactory()
    .AddSerilog(logger);

var config = new WaveePlayerConfig();

var loader = new FileLoader(new NAudioAudioFormatLoader());

var player = new WaveePlayer(
    config: config,
    trackLoader: loader,
    sink: new NAudioSink(),
    logger: loggerFactory.CreateLogger<WaveePlayer>());

var events = player.ReadEventsAsync();
Task.Run(async () =>
{
    await foreach (var @event in events)
    {
        logger.Debug("Event: {@Event}", @event);
    }
});

//var path = "C:\\Users\\ckara\\Downloads\\correct.ogg";
//var path = "C:\\Users\\ckara\\Music\\NewJeans - Attention [320 kbps] (1).mp3";

// var path = "C:\\Users\\ckara\\Music\\Jukjae - 너 없이도 (Without You).mp3";
//
// player.PlayTrack(path, true, TimeSpan.FromMinutes(0).TotalMilliseconds);
//player.PlayTrack(path, true, 0);
bool canExit = false;
while (!canExit)
{
    //read input
    //-play <path> (file or directory)
    //-stop
    //-pause
    //-resume
    //-seek <time> 
    //-volume <volume>
    //-next
    //-previous
    //-exit
    var input = Console.ReadLine();
    var parts = input.Split(' ', 2);
    var command = parts[0];
    switch (command)
    {
        case "-play":
            //trim quotes
            var path = parts[1].Trim('"');
            path = @"" + parts[1];
            //check if path is a directory
            if (Directory.Exists(path))
            {
                //play directory
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".mp3") || s.EndsWith(".ogg") || s.EndsWith(".wav"))
                    .ToList();
                player.PlayTracks(files, true, 0);
            }
            else
            {
                //play file
                player.PlayTrack(path, true, 0);
            }

            break;
        case "-stop":
            //player.Stop();
            break;
        case "-pause":
            player.Pause();
            break;
        case "-resume":
            player.Resume();
            break;
        case "-seek":
            var time = TimeSpan.Parse(parts[1]);
            player.Seek(time);
            break;
        case "-volume":
            var volume = double.Parse(parts[1]);
            //player.SetVolume(volume);
            break;
        case "-next":
            await player.Next();
            break;
        case "-previous":
            await player.Previous();
            break;
        case "-shuffle":
            player.Shuffle();
            break;
        case "-repeat":
            player.NextRepeatState();
            break;
        case "-info":
            //print info about current track and position
            var track = player.GetCurrentTrack();
            var position = player.GetPosition();
            Console.WriteLine("Track: {0}", track);
            Console.WriteLine("Position: {0}", position);
            Console.WriteLine("Shuffle: {0}", player.IsShuffling);
            Console.WriteLine("Repeat: {0}", player.RepeatState);
            break;
        //help
        case "-help":
            Console.WriteLine("Commands:");
            Console.WriteLine("-play <path> (file or directory)");
            Console.WriteLine("-stop");
            Console.WriteLine("-pause");
            Console.WriteLine("-resume");
            Console.WriteLine("-seek <time>");
            Console.WriteLine("-volume <volume>");
            Console.WriteLine("-next");
            Console.WriteLine("-previous");
            Console.WriteLine("-shuffle");
            Console.WriteLine("-repeat");
            Console.WriteLine("-info");
            Console.WriteLine("-help");
            Console.WriteLine("-exit");
            break;
        case "-exit":
            canExit = true;
            return;
    }
}
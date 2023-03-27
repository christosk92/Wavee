// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
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
    //-play <path>
    //-stop
    //-pause
    //-resume
    //-seek <time> 
    //-volume <volume>
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
            var file = new FileInfo(path);

            player.PlayTrack(path, true, 0);
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
        //help
        case "-help":
            Console.WriteLine("Commands:");
            Console.WriteLine("-play <path>");
            Console.WriteLine("-stop");
            Console.WriteLine("-pause");
            Console.WriteLine("-resume");
            Console.WriteLine("-seek <time>");
            Console.WriteLine("-volume <volume>");
            Console.WriteLine("-exit");
            break;
        case "-exit":
            canExit = true;
            return;
    }
}
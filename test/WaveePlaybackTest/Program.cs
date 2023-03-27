// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Wavee.Playback;
using Wavee.Playback.Converters;
using Wavee.Playback.Volume;
using Wavee.Sinks.NAudio;
using ILogger = Serilog.ILogger;

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
var path = "C:\\Users\\ckara\\Music\\NewJeans - Attention [320 kbps] (1).mp3";

player.PlayTrack(path, true, TimeSpan.FromMinutes(1).TotalMilliseconds);
//player.PlayTrack(path, true, 0);
var mn = new ManualResetEvent(false);
mn.WaitOne();
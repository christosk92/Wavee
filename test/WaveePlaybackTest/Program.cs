// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Wavee.Playback;
using Wavee.Playback.Converters;
using Wavee.Playback.Packets;
using Wavee.Playback.Volume;
using Wavee.Sinks.NAudio;
using Wavee.Vorbis;
using Wavee.Vorbis.Decoder;
using Wavee.Vorbis.IO;

var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

var loggerFactory = new LoggerFactory()
    .AddSerilog(logger);

var config = new WaveePlayerConfig();
var converter = new StdAudioConverter();
var volume = new SoftVolume(1.0);
var loader = new FileLoader();

var player = new WaveePlayer(config,
    loader,
    ((channels, sampleRate) => new NAudioSink(AudioFormat.F64, channels, sampleRate))
    , converter, volume, loggerFactory.CreateLogger<WaveePlayer>());

var path = "C:\\Users\\ckara\\Downloads\\test.ogg";

var fss = new FSSource(path);
var mss = new MediaSourceStream(fss, new MediaSourceStreamOptions(4096));
var format = new OggReader(mss, FormatOptions.Default, loggerFactory);
var track = format.DefaultTrack;
var vorbisDecoder = new VorbisDecoder(track.StreamCodecParams);

//player.PlayTrack(path, true, 0);
var mn = new ManualResetEvent(false);
mn.WaitOne();

public class FSSource : MediaSource
{
    private readonly string _path;
    private readonly FileStream _fileStream;

    public FSSource(string path)
    {
        _path = path;
        _fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public override void Flush() => _fileStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _fileStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _fileStream.Seek(offset, origin);

    public override void SetLength(long value) => _fileStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _fileStream.Write(buffer, offset, count);

    public override bool CanRead => _fileStream.CanRead;

    public override bool CanSeek => _fileStream.CanSeek;

    public override bool CanWrite => _fileStream.CanWrite;

    public override long Length => _fileStream.Length;

    public override long Position
    {
        get => _fileStream.Position;
        set => _fileStream.Position = value;
    }
}
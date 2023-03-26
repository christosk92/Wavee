// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Serilog;
using Serilog.Events;
using Wavee.Playback;
using Wavee.Playback.Converters;
using Wavee.Playback.Item;
using Wavee.Playback.Normalisation;
using Wavee.Playback.Packets;
using Wavee.Playback.Volume;
using Wavee.Sinks.NAudio;
using Wavee.UI.Models.Local;
using Wavee.Vorbis;
using Wavee.Vorbis.Decoder;
using Wavee.Vorbis.IO;
using ILogger = Serilog.ILogger;

ILogger logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

var loggerFactory = new LoggerFactory()
    .AddSerilog(logger);

var config = new WaveePlayerConfig();
var converter = new StdAudioConverter();
var volume = new SoftVolume(1.0);
var loader = new VorbisLoader();

var player = new WaveePlayer(config,
    loader,
    ((channels, sampleRate) => new NAudioSink(AudioFormat.F64, channels, sampleRate))
    , converter, volume, loggerFactory.CreateLogger<WaveePlayer>());

var path = "C:\\Users\\ckara\\Downloads\\test.ogg";

player.PlayTrack(path, true, 0);
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

public class VorbisLoader : ITrackLoader
{
    public Task<PlayerLoadedTrackData?> LoadTrackAsync(string trackId, double positionMs)
    {
        //check the format
        using var file = TagLib.File.Create(trackId);
        var bitrate =
            file.Properties.AudioBitrate;

        var audioItem = new LocalTrack
        {
            Title = file.Tag.Title ?? file.Name,
        };
        ILogger logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
            .CreateLogger();

        var loggerFactory = new LoggerFactory()
            .AddSerilog(logger);
        var playbackitem = new PlaybackItem
        {
            Item = audioItem
        };

        file.Dispose();
        var bytesPerSecond = bitrate / 8;

        //assume no normalisation data for now

        //       file.Properties.AudioSampleRate,
        //file.Properties.AudioChannels,
        var normalisationData = NormalisationData.Default;

        var fss = new FSSource(trackId);
        var mss = new MediaSourceStream(fss, new MediaSourceStreamOptions(4096));
        var format = new OggReader(mss, FormatOptions.Default, loggerFactory);
        var track = format.DefaultTrack;
        var vorbisDecoder = new VorbisDecoder(track.StreamCodecParams);

        var decoder = new VorbisTestDecoder(format, vorbisDecoder, fss.Length);

        var duration = TimeSpan.FromSeconds(decoder.TotalTime.TotalSeconds);

        // Don't try to seek past the track's duration.
        // If the position is invalid just start from
        // the beginning of the track.
        var position = TimeSpan.FromMilliseconds(positionMs);
        if (position > duration)
        {
            position = TimeSpan.Zero;
        }

        // Ensure the starting position. Even when we want to play from the beginning,
        // the cursor may have been moved by parsing normalisation data. This may not
        // matter for playback (but won't hurt either), but may be useful for the
        // passthrough decoder.
        decoder.CurrentTime = position;
        if (decoder.CurrentTime != position)
        {
            throw new InvalidOperationException("Failed to seek to the starting position");
        }

        // Ensure streaming mode now that we are ready to play from the requested position.
        var streamLoaderController
            = new StreamLoaderController();

        streamLoaderController.SetStreamMode();

        var isExplicit = false;

        Debug.WriteLine("Loaded track: " + trackId);

        return Task.FromResult(new PlayerLoadedTrackData
        {
            Decoder = new NAudioDecoder(decoder),
            NormalisationData = normalisationData,
            StreamLoaderController = streamLoaderController,
            AudioItem = playbackitem,
            BytesPerSecond = bytesPerSecond,
            DurationMs = duration.TotalMilliseconds,
            IsExplicit = isExplicit,
            StreamPositionMs = decoder.CurrentTime.TotalMilliseconds
        })!;
    }
}

public class VorbisTestDecoder : WaveStream
{
    private readonly OggReader _oggReader;
    private readonly VorbisDecoder _vorbisDecoder;

    public VorbisTestDecoder(OggReader oggReader, VorbisDecoder vorbisDecoder, long length)
    {
        _oggReader = oggReader;
        _vorbisDecoder = vorbisDecoder;
        WaveFormat = new WaveFormat(
            (int)_oggReader.DefaultTrack.StreamCodecParams.SampleRate,
            (int)_oggReader.DefaultTrack.StreamCodecParams.Channels);
        Length = length;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override WaveFormat WaveFormat { get; }
    public override long Length { get; }
    public override long Position { get; set; }
}
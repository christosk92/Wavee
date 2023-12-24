using System.Runtime.InteropServices;
using NAudio.Wave;
using Nito.AsyncEx;
using Wavee.Core.Enums;
using Wavee.Core.Playback;
using Wavee.Interfaces;
using Wavee.Players.NAudio.VorbisDecoder;
using AsyncLock = NeoSmart.AsyncLock.AsyncLock;

namespace Wavee.Players.NAudio;

public sealed class NAudioPlayer : IWaveePlayer
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly IWavePlayer _wavePlayer;
    private readonly WaveFormat waveFormat;
    private readonly AsyncLock _lock = new AsyncLock();
    private readonly BufferedWaveProvider _bufferedWaveProvider;

    private LinkedListNode<Func<ValueTask<IWaveeMediaSource>>>? source = null;

    private TimeSpan? _crossfadeDuration = null;
    private AsyncManualResetEvent _playbackStopped = new AsyncManualResetEvent(true);

    public NAudioPlayer()
    {
        const int sampleRate = 44100;
        const int channels = 2;

        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _wavePlayer = new WaveOutEvent();
        _wavePlayer.Init(_bufferedWaveProvider);

        Task.Factory.StartNew(async () => await StartReadingPackets(), TaskCreationOptions.LongRunning);
    }

    public event EventHandler<WaveePlaybackStateType>? PlaybackStateChanged;
    public event EventHandler<Exception>? PlaybackError;

    public ValueTask Play(WaveePlaybackList playlist)
    {
        //TODO:
        var stream = playlist.Get(0);
        using (_lock.Lock())
        {
            source = null;
            _bufferedWaveProvider.ClearBuffer();
        }

        _playbackStopped.Wait();

        using (_lock.Lock())
        {
            source = stream;
        }

        _wavePlayer.Play();
        return new ValueTask();
    }

    public ValueTask Play(IWaveeMediaSource source)
    {
        var list = WaveePlaybackList.Create(source);
        return Play(list);
    }

    public void Crossfade(TimeSpan crossfadeDuration)
    {
        _crossfadeDuration = crossfadeDuration;
    }

    private async Task StartReadingPackets()
    {
        TimeSpan? stream_one_duration = null;
        TimeSpan? stream_two_duration = null;
        WaveStream? stream_one = null;
        WaveStream? stream_two = null;
        IWaveeMediaSource? sourceOneRaw = null;
        bool notifiedStartedPlaying = false;

        while (!_cts.IsCancellationRequested)
        {
            using (await _lock.LockAsync())
            {
                try
                {
                    if (source is null)
                    {
                        stream_one?.Dispose();
                        stream_two?.Dispose();
                        stream_one = null;
                        stream_two = null;
                        _playbackStopped.Set();
                        await Task.Delay(10, _cts.Token);
                        if (notifiedStartedPlaying)
                            NotifyPlaybackStateChanged(WaveePlaybackStateType.Stopped);
                        notifiedStartedPlaying = false;
                        continue;
                    }

                    _playbackStopped.Reset();

                    if (stream_one is null)
                    {
                        NotifyPlaybackStateChanged(WaveePlaybackStateType.Buffering);

                        sourceOneRaw = await source.Value();
                        sourceOneRaw.BufferingStream += SourceOneRawOnBufferingStream;
                        sourceOneRaw.OnError += SourceOneRawOnOnError;

                        var streamOneRaw = sourceOneRaw.AsStream();
                        stream_one_duration = sourceOneRaw.Duration;
                        stream_one = CreateStream(streamOneRaw);
                    }

                    //Read packet from stream_one
                    var packet = new byte[1024];
                    var copied = stream_one.Read(packet, 0, packet.Length);
                    if (stream_two is not null)
                    {
                        //When we are crossfading, stream_one is always the "main stream", hence this one is fading in.
                        //Stream_two is the outgoing stream, so that one is fading out.
                        var packet_two = new byte[1024];
                        stream_two.Read(packet_two, 0, packet_two.Length);

                        //Crossfade
                        var volumeOut = CalculateCrossfadeOut(_crossfadeDuration.Value,
                            stream_two_duration.Value,
                            stream_two.CurrentTime);

                        var volumeIn = CalculateCrossfadeIn(_crossfadeDuration.Value,
                            stream_one.CurrentTime);

                        //Mix
                        packet = Mix(packet, packet_two, volumeIn, volumeOut);
                    }


                    _bufferedWaveProvider.AddSamples(packet, 0, copied);
                    if (!notifiedStartedPlaying)
                    {
                        NotifyPlaybackStateChanged(WaveePlaybackStateType.Playing);
                        notifiedStartedPlaying = true;
                    }

                    while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
                    {
                        await Task.Delay(10, _cts.Token);
                    }

                    if (copied is 0 || stream_one.CurrentTime >= stream_one_duration.Value)
                    {
                        if (sourceOneRaw is not null)
                        {
                            sourceOneRaw.BufferingStream -= SourceOneRawOnBufferingStream;
                            sourceOneRaw.OnError -= SourceOneRawOnOnError;
                            sourceOneRaw.Dispose();
                        }

                        stream_one = null;
                        source = source.Next;
                        continue;
                    }

                    //Check if we need to crossfade
                    if (_crossfadeDuration is not null
                        && ReachedCrossfadePoint(
                            crossfadeDuration: _crossfadeDuration.Value,
                            duration: sourceOneRaw.Duration,
                            currentTime: stream_one.CurrentTime))
                    {
                        var next = source!.Next;
                        if (next is not null)
                        {
                            var currentStream = stream_one;
                            // Swap streams, main stream is now the new stream
                            stream_two = currentStream;
                            stream_two_duration = stream_one_duration;
                            //Settings this to null will cause the next iteration to create a new stream
                            source = next;
                            stream_one = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    PlaybackError?.Invoke(this, e);
                    NotifyPlaybackStateChanged(WaveePlaybackStateType.Error);
                }
            }
        }
    }

    private void SourceOneRawOnOnError(object? sender, Exception e)
    {
        PlaybackError?.Invoke(this, e);
        NotifyPlaybackStateChanged(WaveePlaybackStateType.Error);
    }

    private void SourceOneRawOnBufferingStream(object? sender, EventArgs e)
    {
        NotifyPlaybackStateChanged(WaveePlaybackStateType.Buffering);
    }

    private WaveePlaybackStateType _state;
    private void NotifyPlaybackStateChanged(WaveePlaybackStateType x)
    {
        if (_state == x)
            return;
        
        _state = x;
        PlaybackStateChanged?.Invoke(this, x);
    }

    private static byte[] Mix(byte[] packet, byte[] packetTwo, double volumeIn, double volumeOut)
    {
        var x_floats = MemoryMarshal.Cast<byte, float>(packet);
        var y_floats = MemoryMarshal.Cast<byte, float>(packetTwo);
        var mixed = new float[x_floats.Length];
        for (var i = 0; i < x_floats.Length; i++)
        {
            var xSample = x_floats[i];
            var ySample = y_floats[i];

            //Crossfade
            var x_volume = xSample * (float)volumeIn;
            var y_volume = ySample * (float)volumeOut;

            mixed[i] = x_volume + y_volume;
        }

        var mixedBytes = MemoryMarshal.Cast<float, byte>(mixed);
        return mixedBytes.ToArray();
    }

    private WaveStream CreateStream(Stream streamOneRaw)
    {
        var format = AudioFormatChecker.CheckFormat(streamOneRaw);
        streamOneRaw.Position = 0;
        switch (format)
        {
            case WaveeAudioFormat.MP3:
            {
                var mp3reader = new Mp3FileReader(streamOneRaw);
                return new WaveChannel32(mp3reader);
                break;
            }
            case WaveeAudioFormat.OGG:
            {
                var oggReader = new VorbisWaveReader(streamOneRaw, true);
                return oggReader;
                break;
            }
        }

        return null;
    }

    private static bool ReachedCrossfadePoint(
        TimeSpan crossfadeDuration,
        TimeSpan duration,
        TimeSpan currentTime)
    {
        var position = currentTime;

        //if crossfade duration is 10 seconds, then we need to start crossfading at 10 seconds before the end of the track
        var crossfadeStart = duration - crossfadeDuration;
        return position >= crossfadeStart;
    }

    private static double CalculateCrossfadeOut(TimeSpan crossfadeDuration, TimeSpan duration, TimeSpan currentTime)
    {
        var time = currentTime;
        var diffrence = duration - time;
        //if this approaches 0, then 0/(x) -> 0, 
        //if this approaches 10 seconds, and crossfadeDur = 10 seconds, then 10/10 -> 1
        var multiplier = (double)(diffrence.TotalSeconds / crossfadeDuration.TotalSeconds);
        multiplier = Math.Clamp(multiplier, 0, 1);
        return multiplier;
    }

    private static double CalculateCrossfadeIn(TimeSpan crossfadeDuration, TimeSpan currentTime)
    {
        var difference = crossfadeDuration - currentTime;
        var progress = (double)(difference.TotalSeconds / crossfadeDuration.TotalSeconds);
        //if diff approaches 0, (meaning we have reached it) then this will result in 0/x -> 0
        //so we need to get the complement of this
        var multiplier = Math.Clamp(progress, 0, 1);
        multiplier = 1 - multiplier;
        return multiplier;
    }
}

internal static class AudioFormatChecker
{
    private static readonly byte[] Mp3MagicNumbers = new byte[] { 0xFF, 0xFB };
    private static readonly byte[] Id3v2MagicNumbers = new byte[] { 0x49, 0x44, 0x33 }; // "ID3"
    private static readonly byte[] OggMagicNumbers = new byte[] { 0x4F, 0x67, 0x67, 0x53 }; // "OggS"

    private static readonly byte[]
        M4aMagicNumbers = new byte[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41 }; // "ftypM4A" at offset 4

    public static WaveeAudioFormat CheckFormat(Stream stream)
    {
        // Check Vorbis first
        stream.Position = 0;
        Span<byte> buffer = stackalloc byte[12]; // For M4A check, we need to read first 12 bytes
        stream.Read(buffer);
        stream.Position = 0;

        if (buffer[..Mp3MagicNumbers.Length].SequenceEqual(Mp3MagicNumbers))
            return WaveeAudioFormat.MP3;

        if (buffer[..Id3v2MagicNumbers.Length].SequenceEqual(Id3v2MagicNumbers))
            return WaveeAudioFormat.MP3;

        if (buffer[..OggMagicNumbers.Length].SequenceEqual(OggMagicNumbers))
            return WaveeAudioFormat.OGG;

        if (buffer[4..(M4aMagicNumbers.Length + 4)].SequenceEqual(M4aMagicNumbers))
            return WaveeAudioFormat.M4A;

        throw new NotSupportedException();
    }
}

internal enum WaveeAudioFormat
{
    MP3,
    OGG,
    M4A
}
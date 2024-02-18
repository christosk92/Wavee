using System.Runtime.InteropServices;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.Core.Decoders.VorbisDecoder.Decoder;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;
using Wavee.Core.Decoders.VorbisDecoder.Packets;

namespace Wavee.Core.Decoders.VorbisDecoder;

public sealed class VorbisWaveReader : WaveStream
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly OggReader _oggReader;
    private readonly Decoder.VorbisDecoder _decoder;

    public VorbisWaveReader(Stream stream, bool leaveOpen)
    {
        var oggReaderMaybe = OggReader.TryNew(
            source: new MediaSourceStream(stream, MediaSourceStreamOptions.Default),
            FormatOptions.Default with { EnableGapless = true }
        );
        if (oggReaderMaybe.IsFaulted)
        {
            var err = oggReaderMaybe.Match(
                Succ: _ => throw new Exception("Unexpected success"),
                Fail: e => e
            );
            throw err;
        }

        _oggReader = oggReaderMaybe.Match(x => x, e => throw e);

        var track = _oggReader.DefaultTrack.ValueUnsafe();
        var decoderMaybe = Decoder.VorbisDecoder.TryNew(track.CodecParams, new DecoderOptions());
        if (decoderMaybe.IsFaulted)
        {
            var error = decoderMaybe.Match(
                Succ: _ => throw new Exception("Unexpected success"),
                Fail: e => e
            );
            throw error;
        }

        _decoder = decoderMaybe.Match(x => x, e => throw e);

        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
            sampleRate: (int)track.CodecParams.SampleRate.ValueUnsafe(),
            channels: track.CodecParams.ChannelsCount.ValueUnsafe()
        );

        TotalTime = _oggReader.TotalTime.ValueUnsafe();
    }

    private Option<(byte[], TimeSpan)> _currentPacket = Option<(byte[], TimeSpan)>.None;
    private int _currentPacketOffset = 0; // to keep track of how much of the current packet is consumed

    public override TimeSpan CurrentTime
    {
        get => _oggReader.Position.Match(
            None: () => _currentPacket.Map(f => f.Item2),
            Some: x => x
        ).IfNone(TimeSpan.Zero);
        set => SeekToBytesPos((long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond));
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        _semaphore.Wait();
        try
        {
            int bytesRead = 0; // this will store total bytes read into buffer

            while (bytesRead < count)
            {
                // If current packet is None or fully consumed, fetch a new packet
                if (!_currentPacket.IsSome || _currentPacketOffset >= _currentPacket.ValueUnsafe().Item1.Length)
                {
                    _currentPacket = FetchNewPacket();
                    _currentPacketOffset = 0;

                    // If no new packets are available, break out of loop
                    if (!_currentPacket.IsSome)
                        break;
                }

                var currentPacketValue = _currentPacket.ValueUnsafe();
                // Calculate how much bytes can be read from the current packet
                int bytesToRead = Math.Min(count - bytesRead,
                    currentPacketValue.Item1.Length - _currentPacketOffset);

                // Copy data from current packet to buffer
                Array.Copy(currentPacketValue.Item1, _currentPacketOffset, buffer, offset + bytesRead, bytesToRead);

                // Update counters
                bytesRead += bytesToRead;
                _currentPacketOffset += bytesToRead;
            }

            return bytesRead;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            //Log.Error(e, "Error reading vorbis stream");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override TimeSpan TotalTime { get; }

    private Option<(byte[], TimeSpan)> FetchNewPacket()
    {
        // Logic to fetch new packet from _oggReader
        // If a new packet is available return Option<byte[]>.Some(packetData)
        // If no more packets are available return Option<byte[]>.None
        var oggpacketMaybe = _oggReader.NextPacket()
            .Match(Succ: x => Option<OggPacket>.Some(x), Fail: e =>
            {
                Console.WriteLine(e);
                //Log.Error(e, "Error reading packet");
                return Option<OggPacket>.None;
            });

        if (oggpacketMaybe.IsNone)
        {
            return Option<(byte[], TimeSpan)>.None;
        }

        var oggpacket = oggpacketMaybe.ValueUnsafe();
        // Decode the packet into audio samples.
        var decodeResult = _decoder.Decode(oggpacket);
        if (decodeResult.IsFaulted)
        {
            Console.WriteLine(decodeResult.Match(Succ: _ => throw new Exception("Unexpected success"), Fail: e => e));
            //Log.Error(decodeResult.Match(Succ: _ => throw new Exception("Unexpected success"), Fail: e => e),
            //  "Error decoding packet");
            return Option<(byte[], TimeSpan)>.None;
        }

        var samples = decodeResult.Match(x => x, e => throw e);
        var bytes = MemoryMarshal.Cast<float, byte>(samples).ToArray();
        var pos = _oggReader.PositionOfPacket(oggpacket).IfNone(TimeSpan.Zero);
        return Option<(byte[], TimeSpan)>.Some((bytes, pos));
    }

    public override WaveFormat WaveFormat { get; }

    public override long Length
    {
        get => (long)_oggReader.TotalBytes.IfNone(0);
    }

    public override long Position
    {
        get => CalculateBytesPosition(_oggReader.Position);
        set => SeekToBytesPos(value);
    }

    private long CalculateBytesPosition(Option<TimeSpan> oggReaderPosition)
    {
        if (oggReaderPosition.IsNone)
            return 0;

        var position = oggReaderPosition.ValueUnsafe();
        var bytesPerSecond = WaveFormat.AverageBytesPerSecond;
        return (long)(position.TotalSeconds * bytesPerSecond);
    }

    private void SeekToBytesPos(long value)
    {
        _semaphore.Wait();
        var bytesPerSecond = WaveFormat.AverageBytesPerSecond;
        var position = TimeSpan.FromSeconds(value / (double)bytesPerSecond);
        _oggReader.Seek(SeekMode.Accurate, position);
        _semaphore.Release();
    }
}
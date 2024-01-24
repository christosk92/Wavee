using System.Runtime.InteropServices;
using AsyncKeyedLock;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Serilog;
using Wavee.Vorbis;
using Wavee.Vorbis.Decoder;
using Wavee.Vorbis.Infrastructure.Stream;
using Wavee.Vorbis.Packets;

namespace Wavee.VorbisDecoder;

public sealed class VorbisWaveReader : WaveStream
{
    private readonly AsyncNonKeyedLocker _semaphore = new(1);
    private readonly OggReader _oggReader;
    private readonly Vorbis.Decoder.VorbisDecoder _decoder;

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
        var decoderMaybe = Vorbis.Decoder.VorbisDecoder.TryNew(track.CodecParams, new DecoderOptions());
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

    private Option<byte[]> _currentPacket = Option<byte[]>.None;
    private int _currentPacketOffset = 0; // to keep track of how much of the current packet is consumed

    public override int Read(byte[] buffer, int offset, int count)
    {
        using (_semaphore.Lock())
        {
            try
            {
                int bytesRead = 0; // this will store total bytes read into buffer

                while (bytesRead < count)
                {
                    // If current packet is None or fully consumed, fetch a new packet
                    if (!_currentPacket.IsSome || _currentPacketOffset >= _currentPacket.ValueUnsafe().Length)
                    {
                        _currentPacket = FetchNewPacket();
                        _currentPacketOffset = 0;

                        // If no new packets are available, break out of loop
                        if (!_currentPacket.IsSome)
                            break;
                    }

                    // Calculate how much bytes can be read from the current packet
                    int bytesToRead = Math.Min(count - bytesRead,
                        _currentPacket.ValueUnsafe().Length - _currentPacketOffset);

                    // Copy data from current packet to buffer
                    Array.Copy(_currentPacket.ValueUnsafe(), _currentPacketOffset, buffer, offset + bytesRead, bytesToRead);

                    // Update counters
                    bytesRead += bytesToRead;
                    _currentPacketOffset += bytesToRead;
                }

                return bytesRead;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error reading vorbis stream");
                throw;
            }
        }
    }

    public override TimeSpan TotalTime { get; }

    private Option<byte[]> FetchNewPacket()
    {
        // Logic to fetch new packet from _oggReader
        // If a new packet is available return Option<byte[]>.Some(packetData)
        // If no more packets are available return Option<byte[]>.None
        var oggpacketMaybe = _oggReader.NextPacket()
            .Match(Succ: x => Option<OggPacket>.Some(x), Fail: e =>
            {
                Log.Error(e, "Error reading packet");
                return Option<OggPacket>.None;
            });

        if (oggpacketMaybe.IsNone)
        {
            return Option<byte[]>.None;
        }
        
        var oggpacket = oggpacketMaybe.ValueUnsafe();
        // Decode the packet into audio samples.
        var decodeResult = _decoder.Decode(oggpacket);
        if (decodeResult.IsFaulted)
        {
            Log.Error(decodeResult.Match(Succ: _ => throw new Exception("Unexpected success"), Fail: e => e),
                "Error decoding packet");
            return Option<byte[]>.None;
        }
        
        var samples = decodeResult.Match(x => x, e => throw e);
        var bytes = MemoryMarshal.Cast<float, byte>(samples).ToArray();
        return Option<byte[]>.Some(bytes);
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
        using (_semaphore.Lock())
        {
            var bytesPerSecond = WaveFormat.AverageBytesPerSecond;
            var position = TimeSpan.FromSeconds(value / (double)bytesPerSecond);
            _oggReader.Seek(SeekMode.Accurate, position);
        }
    }
}
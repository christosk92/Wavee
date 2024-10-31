using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using Wavee.Core.Decoders.VorbisDecoder;
using Wavee.Core.Decoders.VorbisDecoder.Decoder;
using Wavee.Core.Decoders.VorbisDecoder.Format;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;
using Wavee.Core.Decoders.VorbisDecoder.Packets;

namespace Wavee.Core.Decoders
{
    public sealed class VorbisSampleProvider : ISampleProvider, IDisposable, IAsyncDisposable
    {
        private float[] _buffer = Array.Empty<float>();
        private readonly Stream _stream;
        private readonly OggReader _oggReader;
        private VorbisDecoder.Decoder.VorbisDecoder _decoder;
        private bool _prefetchRequested;

        // Lock to synchronize Read and Seek operations
        private readonly object _lock = new object();

        // Indicates if a seek operation is in progress
        private bool _isSeeking = false;
        private CodecParameters _codecParameters;

        public Stream InnerStream => _stream;

        public VorbisSampleProvider(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
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
            _codecParameters = track.CodecParams;
            var decoderMaybe = VorbisDecoder.Decoder.VorbisDecoder.TryNew(track.CodecParams, new DecoderOptions());
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

            ReadNextPacket();
        }

        public TimeSpan? TotalTime { get; set; }

        public WaveFormat WaveFormat { get; }
        public TimeSpan Position { get; private set; }
        public event EventHandler? PrefetchedRequested;
        public event EventHandler? EndOfStream;

        /// <summary>
        /// Reads samples from the decoder into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read samples into.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The number of samples to read.</param>
        /// <returns>The number of samples read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                // If a seek is in progress, wait until it's completed
                while (_isSeeking)
                {
                    Monitor.Wait(_lock);
                }

                int samplesRead = 0;
                while (samplesRead < count)
                {
                    if (_buffer.Length == 0)
                    {
                        var packetRead = ReadNextPacket();
                        if (!packetRead)
                        {
                            EndOfStream?.Invoke(this, EventArgs.Empty);
                            break; // No more data
                        }
                    }

                    if (_buffer.Length == 0)
                    {
                        break; // No more data
                    }

                    int samplesToCopy = Math.Min(count - samplesRead, _buffer.Length);
                    Array.Copy(_buffer, 0, buffer, offset + samplesRead, samplesToCopy);
                    samplesRead += samplesToCopy;

                    if (samplesToCopy < _buffer.Length)
                    {
                        // Retain leftover samples
                        float[] newBuffer = new float[_buffer.Length - samplesToCopy];
                        Array.Copy(_buffer, samplesToCopy, newBuffer, 0, newBuffer.Length);
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = Array.Empty<float>();
                    }
                }

                return samplesRead;
            }
        }

        /// <summary>
        /// Seeks to the specified position in the audio stream.
        /// </summary>
        /// <param name="position">The position to seek to.</param>
        public void Seek(TimeSpan position)
        {
            lock (_lock)
            {
                _isSeeking = true;

                try
                {
                    // Perform seeking using OggReader
                    var seekedToResult = _oggReader.Seek(SeekMode.Coarse, position);
                    if (!seekedToResult.IsSuccess)
                    {
                    }

                    // Reset the decoder's state
                    ResetDecoder();

                    // Clear the buffer to remove leftover samples
                    _buffer = Array.Empty<float>();

                    // Read the first packet after seeking to prime the buffer
                    ReadNextPacket();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during seek: {ex.Message}");
                    throw;
                }
                finally
                {
                    _isSeeking = false;
                    Monitor.PulseAll(_lock); // Notify any waiting threads
                }
            }
        }

        private void ResetDecoder()
        {
            // Re-initialize the decoder
            var decoderMaybe = VorbisDecoder.Decoder.VorbisDecoder.TryNew(_codecParameters, new DecoderOptions());
            if (decoderMaybe.IsFaulted)
            {
                throw decoderMaybe.Error();
            }

            _decoder = decoderMaybe.Match(x => x, e => throw e);
        }

        /// <summary>
        /// Reads the next packet from the Ogg stream and decodes it into samples.
        /// </summary>
        private bool ReadNextPacket()
        {
            var packetMaybe = _oggReader.NextPacket()
                .Match(Succ: Option<OggPacket>.Some, Fail: e =>
                {
                    Console.WriteLine($"Error reading packet: {e.Message}");
                    return Option<OggPacket>.None;
                });
            if (packetMaybe.IsNone)
            {
                return false;
            }

            var packet = packetMaybe.ValueUnsafe();
            Position = _oggReader.PositionOfPacket(packet).IfNone(TimeSpan.Zero);
            // Decode the packet into audio samples.
            var decodeResult = _decoder.Decode(packet);
            if (decodeResult.IsFaulted)
            {
                Console.WriteLine(
                    $"Decoding failed: {decodeResult.Match(Succ: _ => throw new Exception("Unexpected success"), Fail: e => e)}");
                return false;
            }

            //if we are 15 seconds from the end of the stream, prefetch the next stream
            if (TotalTime is not null && !_prefetchRequested)
            {
                var diff = TotalTime.Value - Position;
                if (diff.TotalSeconds < 15)
                {
                    PrefetchedRequested?.Invoke(this, EventArgs.Empty);
                    _prefetchRequested = true;
                }
            }


            _buffer = decodeResult.Match(x => x, e => throw e);
            return true;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync();
        }
    }
}
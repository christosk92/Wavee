using Windows.Foundation;
using Windows.Storage.Streams;
using NeoSmart.AsyncLock;
using Wavee.Interfaces;
using Wavee.Playback.Player;
using Wavee.Playback.Streaming;

namespace Wavee.UI.Player;

internal sealed class LazyStreamReference : IRandomAccessStreamReference
{
    private readonly RequestAudioStreamForTrackAsync _requestAudioStreamForTrack;
    private readonly WaveePlayerMediaItem _track;
    private readonly AsyncLock _lock = new();
    
    public LazyStreamReference(RequestAudioStreamForTrackAsync requestAudioStreamForTrack, WaveePlayerMediaItem track)
    {
        _track = track;
        _requestAudioStreamForTrack = requestAudioStreamForTrack;
    }

    public AudioStream Stream { get; set; }

    public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        => Open().AsAsyncOperation();
    private async Task<IRandomAccessStreamWithContentType> Open()
    {
        using (await _lock.LockAsync())
        {
            if (Stream is null)
            {
                var stream = await _requestAudioStreamForTrack(_track, CancellationToken.None);
                await stream.InitializeAsync(CancellationToken.None);
                _track.Stream = stream;
                // if (stream is WaveeAudioStream s)
                // {
                //     await s.AwaitFulInit;
                // }

                Stream = stream;
            }

            var randomAccessStream = Stream.AsRandomAccessStream();
            return new f(randomAccessStream);
        }
    }

    private sealed class f : IRandomAccessStreamWithContentType
    {
        private readonly IRandomAccessStream _stream;

        public f(IRandomAccessStream stream)
        {
            _stream = stream;
        }


        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count,
            InputStreamOptions options)
        {
            return _stream.ReadAsync(buffer, count, options);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            return _stream.WriteAsync(buffer);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return _stream.FlushAsync();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            return _stream.GetInputStreamAt(position);
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            return _stream.GetOutputStreamAt(position);
        }

        public void Seek(ulong position)
        {
            _stream.Seek(position);
        }

        public IRandomAccessStream CloneStream()
        {
            return _stream.CloneStream();
        }

        public bool CanRead => _stream.CanRead;

        public bool CanWrite => _stream.CanWrite;

        public ulong Position => _stream.Position;

        public ulong Size
        {
            get => _stream.Size;
            set => _stream.Size = value;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public string ContentType { get; } = "audio/ogg";
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Playback.Streaming;

public abstract class AudioStream : Stream
{
    protected AudioStream(WaveePlayerMediaItem mediaItem)
    {
        MediaItem = mediaItem;
    }

    public abstract Task InitializeAsync(CancellationToken cancellationToken);

    public TimeSpan TotalDuration { get; protected set; }
    
    // Abstract Stream members to be implemented by derived classes
    public abstract override bool CanRead { get; }
    public abstract override bool CanSeek { get; }
    public abstract override bool CanWrite { get; }
    public abstract override long Length { get; }
    public abstract override long Position { get; set; }
    public SpotifyPlayableItem? Track { get; protected set; }
    public WaveePlayerMediaItem MediaItem { get; }

    public abstract override void Flush();
    public abstract override int Read(byte[] buffer, int offset, int count);
    public abstract override long Seek(long offset, SeekOrigin origin);
    public abstract override void SetLength(long value);
    public abstract override void Write(byte[] buffer, int offset, int count);

    // public abstract ISampleProviderExtended CreateSampleProvider();
}
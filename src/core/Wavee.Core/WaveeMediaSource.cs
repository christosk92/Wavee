namespace Wavee.Core;

public abstract class WaveeMediaSource : Stream
{
    protected WaveeMediaSource(long length, TimeSpan duration)
    {
        Length = length;
        Duration = duration;
    }
    
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }
    public TimeSpan Duration { get; }

    public override void Flush()
    {
        throw new NotSupportedException();
    }
}
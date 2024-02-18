namespace Wavee.Core;

public abstract class WaveeMediaSource : Stream
{
    protected WaveeMediaSource(long length)
    {
        Length = length;
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
    public override void Flush()
    {
        throw new NotSupportedException();
    }
}
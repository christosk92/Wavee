namespace Wavee.Vorbis.Infrastructure.Io;

internal sealed class UnderrunError : EndOfStreamException
{
    public UnderrunError() : base("buffer underrun")
    {
    }
}
namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;

internal sealed class UnderrunError : EndOfStreamException
{
    public UnderrunError() : base("buffer underrun")
    {
    }
}
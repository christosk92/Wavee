namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

public sealed class DecodeError : Exception
{
    public DecodeError(string message) : base(message)
    {
    }
}
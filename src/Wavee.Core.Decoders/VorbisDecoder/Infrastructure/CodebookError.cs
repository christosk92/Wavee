namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

internal sealed class CodebookError : Exception
{
    public CodebookError(string message) : base(message)
    {
    }
}
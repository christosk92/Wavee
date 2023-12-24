namespace Wavee.Vorbis.Infrastructure;

internal sealed class CodebookError : Exception
{
    public CodebookError(string message) : base(message)
    {
    }
}
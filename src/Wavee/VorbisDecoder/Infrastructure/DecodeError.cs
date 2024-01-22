namespace Wavee.Vorbis.Infrastructure;

public sealed class DecodeError : Exception
{
    public DecodeError(string message) : base(message)
    {
    }
}
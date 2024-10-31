namespace Wavee.Exceptions;

public sealed class WaveeOperationCanceledException : Exception
{
    public WaveeOperationCanceledException(string message, OperationCanceledException inner) : base(message, inner)
    {
    }
}
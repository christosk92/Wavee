namespace Wavee.Interfaces;

public interface IWaveeMediaSource : IDisposable
{
    Stream AsStream();
    TimeSpan Duration { get; }

    event EventHandler<TaskCompletionSource> BufferingStream;
    event EventHandler<Exception> OnError;
    event EventHandler<Exception>? ResumedFromError;
}
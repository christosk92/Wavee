using Wavee.Interfaces.Models;

namespace Wavee.Interfaces;

public interface IWaveeMediaSource : IDisposable
{
    IWaveePlayableItem Item { get; }
    
    Stream AsStream();
    TimeSpan Duration { get; }

    event EventHandler<TaskCompletionSource> BufferingStream;
    event EventHandler<Exception> OnError;
    event EventHandler<Exception>? ResumedFromError;
}
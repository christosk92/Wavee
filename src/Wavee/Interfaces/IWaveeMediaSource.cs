namespace Wavee.Interfaces;

public interface IWaveeMediaSource : IDisposable
{
    Stream AsStream();
    TimeSpan Duration { get;  }

    event EventHandler BufferingStream;
    event EventHandler<Exception> OnError;
}
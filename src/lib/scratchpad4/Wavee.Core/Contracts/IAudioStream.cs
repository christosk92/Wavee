namespace Wavee.Core.Contracts;

public interface IAudioStream
{
    ITrack Track { get; }
    Stream AsStream();
}
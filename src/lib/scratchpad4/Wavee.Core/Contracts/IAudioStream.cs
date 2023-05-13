namespace Wavee.Core.Contracts;

public interface IAudioStream
{
    ITrack Track { get; }
    Option<string> Uid { get; }
    Stream AsStream();
}
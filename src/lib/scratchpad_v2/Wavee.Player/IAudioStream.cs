namespace Wavee.Player;

public interface IAudioStream
{
    string TrackId { get; }
    int TotalDuration { get; }
    Stream AsStream();
}
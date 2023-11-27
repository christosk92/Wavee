namespace Wavee.Domain.Playback.Player;

public interface IWaveeMediaSource
{
    ValueTask<Stream> CreateStream();
    TimeSpan Duration { get; }
    //TimeSpan CurrentTime { get; }
}
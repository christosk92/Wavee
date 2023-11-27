namespace Wavee.Domain.Playback.Player;

public interface IWaveeMediaSource : IDisposable
{
    IReadOnlyDictionary<string, string> Metadata { get; }

    ValueTask<Stream> CreateStream();

    TimeSpan Duration { get; }
    //TimeSpan CurrentTime { get; }
}
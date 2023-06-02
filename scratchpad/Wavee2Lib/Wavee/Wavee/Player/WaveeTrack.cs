using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Player;

public class WaveeTrack
{
    public WaveeTrack(Stream audioStream, string title, AudioId id, HashMap<string, string> metadata, TimeSpan duration)
    {
        AudioStream = audioStream;
        Title = title;
        Id = id;
        Metadata = metadata;
        Duration = duration;
    }

    public Stream AudioStream { get; }
    public string Title { get; }
    public AudioId Id { get; }
    public TimeSpan Duration { get; }
    public HashMap<string, string> Metadata { get; }
}
using LanguageExt;

namespace Wavee.Player.Ctx;

public class WaveeTrack
{
    public WaveeTrack(Stream audioStream, string title, string id, HashMap<string, object> metadata, TimeSpan duration,
        Option<NormalisationData> normalisationData)
    {
        AudioStream = audioStream;
        Title = title;
        Id = id;
        Metadata = metadata;
        Duration = duration;
    }

    public Stream AudioStream { get; }
    public string Title { get; }
    public string Id { get; }
    public TimeSpan Duration { get; }
    public HashMap<string, object> Metadata { get; }
}
namespace Wavee.Contracts.Interfaces.Contracts;

public interface IPlayableItem : IItem
{
    IContributor MainContributor { get; }
    TimeSpan Duration { get; }
    string? SmallestImage { get; }
    IAudioFile[] AudioFiles { get; }
}

public interface IAudioFile
{
    string Id { get; }
    AudioFileType Type { get; }
    AudioFileQuality Quality { get; }
}

public enum AudioFileType
{
    Vorbis,
    Mp3,
    Unknown
}
public enum AudioFileQuality
{
    Low,
    Medium,
    High
}
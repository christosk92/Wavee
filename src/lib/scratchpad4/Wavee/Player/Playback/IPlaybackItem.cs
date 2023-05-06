namespace Wavee.Player.Playback;

public interface IPlaybackItem
{
    string Title { get; }
    Option<string> LargeImage { get; }
    int Duration { get; }
    Seq<DescriptionaryItem> Descriptions { get; }
}

public readonly record struct DescriptionaryItem(string Name);
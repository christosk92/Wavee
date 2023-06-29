using Wavee.Id;

namespace Wavee.UI.Common;

public interface ICardViewModel
{
    string Id { get; }
    string Title { get; }
    string? Image { get; }
    bool ImageIsIcon { get; }
    string? Subtitle { get; }
    bool HasSubtitle { get; }
}
public sealed class CardViewModel : ICardViewModel
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Subtitle { get; init; }
    public string? Caption { get; init; }
    public string? Image { get; init; }
    public bool ImageIsIcon { get; init; }
    public bool IsArtist { get; init; }
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    public AudioItemType Type { get; init; }
}

public sealed class PodcastEpisodeCardViewModel : ICardViewModel
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string? Image { get; init; }
    public bool ImageIsIcon => false;
    public string? Subtitle => Show;
    public bool HasSubtitle => true;
    public bool Started { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan Progress { get; init; }
    public string Show { get; init; }
    public string? PodcastDescription { get; init; }
    public DateTimeOffset ReleaseDate { get; init; }
}
namespace Wavee.UI.Common;

public sealed class CardViewModel
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Subtitle { get; init; }
    public string? Caption { get; init; }
    public string? Image { get; init; }
    public bool ImageIsIcon { get; init; }
    public bool IsArtist { get; init; }
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
}
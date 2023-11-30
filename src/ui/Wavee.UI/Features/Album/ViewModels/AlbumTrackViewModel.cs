namespace Wavee.UI.Features.Album.ViewModels;

public sealed class AlbumTrackViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int Number { get; init; }

    public string DurationString => Duration.ToString(@"mm\:ss");
}
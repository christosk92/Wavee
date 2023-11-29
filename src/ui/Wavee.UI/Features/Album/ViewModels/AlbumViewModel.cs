namespace Wavee.UI.Features.Album.ViewModels;

public sealed class AlbumViewModel
{
    public required string Id { get; init; }
    public required string BigImageUrl { get; init; }
    public required string Name { get; init; }
    public required uint TotalSongs { get; init; }
    public required TimeSpan Duration { get; init; }
    public required ushort Year { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyCollection<AlbumTrackViewModel> Tracks { get; init; }

    public string FormatToDurationString(TimeSpan timeSpan)
    {
        //3 min, 10 sec
        var totalHrs = timeSpan.Hours;
        var totalMins = timeSpan.Minutes;
        var totalSecs = timeSpan.Seconds;
        if (totalHrs > 0)
        {
            return $"{totalHrs} hr, {totalMins} min, {totalSecs} sec";
        }
        else if (totalMins > 0)
        {
            return $"{totalMins} min, {totalSecs} sec";
        }
        else
        {
            return $"{totalSecs} sec";
        }
    }
}
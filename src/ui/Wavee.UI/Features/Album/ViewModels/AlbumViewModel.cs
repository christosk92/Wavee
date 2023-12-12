using Wavee.UI.Features.Artist.Queries;

namespace Wavee.UI.Features.Album.ViewModels;

public sealed class AlbumViewModel
{
    public string Id { get; set; }
    public string BigImageUrl { get; set; }
    public string Name { get; set; }
    public uint TotalSongs { get; set; }
    public TimeSpan Duration { get; set; }
    public ushort Year { get; set; }
    public string Type { get; set; }
    public DiscographyGroupType GroupType { get; init; }
    public IReadOnlyCollection<AlbumTrackViewModel> Tracks { get; set; }
    public string MediumImageUrl { get; set; }
}
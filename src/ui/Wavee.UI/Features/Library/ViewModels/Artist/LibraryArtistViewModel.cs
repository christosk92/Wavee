namespace Wavee.UI.Features.Library.ViewModels.Artist;

public sealed class LibraryArtistViewModel
{
    public string Name { get; init; }
    public string Id { get; init; }
    public string BigImageUrl { get; init; }
    public string SmallImageUrl { get; init; }
    public string MediumImageUrl { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Features.Album.ViewModels;

namespace Wavee.UI.Features.Library.ViewModels.Artist;

public sealed class LibraryArtistViewModel : ObservableObject
{
    private uint? _totalAlbums;
    public string Name { get; init; }
    public string Id { get; init; }
    public string BigImageUrl { get; init; }
    public string SmallImageUrl { get; init; }
    public string MediumImageUrl { get; init; }
    public DateTimeOffset AddedAt { get; init; }

    public ObservableCollection<AlbumViewModel> Albums { get; } = new();

    public uint? TotalAlbums
    {
        get => _totalAlbums;
        set => SetProperty(ref _totalAlbums, value);
    }
}
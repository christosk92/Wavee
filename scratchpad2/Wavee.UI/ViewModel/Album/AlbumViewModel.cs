using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.ViewModel.Album;

public sealed class AlbumViewModel : ObservableObject
{
    private string? _albumImage;

    public string? AlbumImage
    {
        get => _albumImage;
        set => SetProperty(ref _albumImage, value);
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Client.Artist;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Artist;

public sealed class ArtistViewModel : ObservableObject
{
    private readonly UserViewModel _userViewModel;
    private WaveeUIArtistView? _artist;

    public ArtistViewModel(UserViewModel userViewModel)
    {
        _userViewModel = userViewModel;
    }

    public WaveeUIArtistView? Artist
    {
        get => _artist;
        private set => SetProperty(ref _artist, value);
    }
    public async Task Fetch(string id, CancellationToken ct)
    {
        var client = _userViewModel.Client.Artist;
        Artist = await client.GetArtist(id, ct);
    }
}
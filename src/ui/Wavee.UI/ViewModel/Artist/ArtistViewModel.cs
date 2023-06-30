using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Client.Artist;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Artist;

public sealed class ArtistViewModel : ObservableObject
{
    private readonly UserViewModel _userViewModel;
    private WaveeUIArtistView? _artist;
    private bool _following;

    public ArtistViewModel(UserViewModel userViewModel)
    {
        _userViewModel = userViewModel;
    }

    public WaveeUIArtistView? Artist
    {
        get => _artist;
        private set
        {
            if (SetProperty(ref _artist, value))
            {
                this.OnPropertyChanged(nameof(Header));
                this.OnPropertyChanged(nameof(MonthlyListenersText));
            }
        }
    }

    public string? Header => Artist?.HeaderImage is not null &&
                             Artist.HeaderImage.IsSome
        ? Artist.HeaderImage.ValueUnsafe().Url
        : null;

    public string? MonthlyListenersText
    {
        get
        {
            if (Artist is not null) return $"{Artist.MonthlyListeners:N0} monthly listeners";
            return null;
        }
    }

    public bool IsFollowing
    {
        get => _following;
        set => SetProperty(ref _following, value);
    }

    public async Task Fetch(string id, CancellationToken ct)
    {
        var client = _userViewModel.Client.Artist;
        Artist = await client.GetArtist(id, ct);
    }
}
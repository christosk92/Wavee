using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.UI.Client.Artist;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Shell;

namespace Wavee.UI.ViewModel.Artist;

public sealed class ArtistViewModel : ObservableObject
{
    private UserViewModel _userViewModel;
    private WaveeUIArtistView? _artist;
    private bool _following;
    private IDisposable? _disposable;
    private bool _loading;

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

    public bool Loading
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
    }

    public async Task Fetch(string id, CancellationToken ct)
    {
        var client = _userViewModel.Client.Artist;
        Artist = await Task.Run(async () => await client.GetArtist(id, ct), ct);
        IsFollowing = ShellViewModel.Instance.Library.InLibrary(Artist.Id);
    }

    public void CreateListener()
    {
        _disposable = ShellViewModel.Instance.Library
            .CreateListener(Artist!.Id)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v =>
            {
                IsFollowing = v;
            });
    }
    public void Destroy()
    {
        _disposable?.Dispose();
        _userViewModel = null!;
    }
}
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Eum.Helpers;
using Eum.UI.Users;
using Eum.UI.ViewModels.NavBar;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Playlists;
using Eum.Users;
using ReactiveUI;
using Guard = Eum.UI.Helpers.Guard;

namespace Eum.UI.ViewModels.Users;

public abstract partial class UserViewModelBase : NavBarItemViewModel, IComparable<UserViewModelBase>
{
	[AutoNotify] private bool _isLoading;

    private string _title;
    private readonly SourceList<PlaylistViewModel> _playlistsSourceList = new();
    private readonly ObservableCollectionExtended<PlaylistViewModel> _playlists = new();

    protected UserViewModelBase(EumUser wallet)
	{
		User = Guard.NotNull(nameof(wallet), wallet);

		_title = User.UserName;

		OpenCommand = ReactiveCommand.Create(() => Navigate().To(this, NavigationMode.Clear));

        _playlistsSourceList
            .Connect()
            .Sort(SortExpressionComparer<PlaylistViewModel>.Descending(i => i.Order))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(_playlists)
            .Subscribe();

        EnumeratePlaylists();
        // this.WhenAnyValue(x => x.IsCoinJoining)
        // 	.Skip(1)
        // 	.Subscribe(_ => MainViewModel.Instance.InvalidateIsCoinJoinActive());
    }
    private void EnumeratePlaylists()
    {
        foreach (var eumPlaylist in User.UserDetailProvider.Playlists ?? Array.Empty<EumPlaylist>())
        {
            eumPlaylist.User = User;
            _playlistsSourceList.Add(PlaylistViewModel.Create(eumPlaylist));
        }
    }
    public override string Title
	{
		get => _title;
		protected set => this.RaiseAndSetIfChanged(ref _title, value);
	}

	public EumUser User { get; }

	public string UserId => User.UserId;

	public bool IsLoggedIn => User.IsLoggedIn;
	public string? Image => User.ProfilePicture;
	public bool HasImage => !string.IsNullOrEmpty(Image);

	public int CompareTo(UserViewModelBase? other)
	{
		if (other is null)
		{
			return -1;
		}

		var result = other.IsLoggedIn.CompareTo(IsLoggedIn);

		if (result == 0)
		{
			result = string.Compare(Title, other.Title, StringComparison.Ordinal);
		}

		return result;
	}

	public override string ToString() => UserId;

	public static UserViewModelBase Create(EumUser wallet)
	{
		return wallet.UserDetailProvider.Service == ServiceType.Local
				? new LocalUserViewModel(wallet)
				: null;
	}


    public event EventHandler<PlaylistViewModel>? PlaylistAdded;
    public ObservableCollection<PlaylistViewModel> Playlists => _playlists;

    private object Lock = new object();
    public PlaylistViewModel AddPlaylist(string? title, string? image, EumUser forUser)
    {
        var newPlaylist = new EumPlaylist
        {
            Name = title,
            Id = Guid.NewGuid(),
            User = forUser,
            ImagePath = image
        };
        var vm = PlaylistViewModel.Create(newPlaylist);
        lock (Lock)
        {
            _playlistsSourceList.Add(vm);
        }

        if (forUser.UserDetailProvider.Playlists == null)
        {
            forUser.UserDetailProvider.Playlists = new[]
            {
                newPlaylist
            };
        }
        else
        {
            var playlists = forUser.UserDetailProvider.Playlists;
            Array.Resize(ref playlists, playlists.Length + 1);
            playlists[^1] = newPlaylist;
            forUser.UserDetailProvider.Playlists = playlists;
        }

        PlaylistAdded?.Invoke(this, vm);
        return vm;
    }
}

public class LocalUserViewModel : UserViewModelBase
{
    public LocalUserViewModel(EumUser wallet) : base(wallet)
    {
    }
}
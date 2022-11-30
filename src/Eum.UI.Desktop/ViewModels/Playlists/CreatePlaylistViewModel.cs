using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Users;
using Eum.Users;

namespace Eum.UI.ViewModels.Playlists
{
    public partial class CreatePlaylistViewModel  : RoutableViewModel
    {
        [AutoNotify]
        private string? _playlistTitle;
        [AutoNotify]
        private string? _browsedPicturePath;

        public CreatePlaylistViewModel()
        {
            SyncWithServices = new ServiceTypeCheckedHolder[]
            {
                new ServiceTypeCheckedHolder
                {
                    Service = ServiceType.Spotify
                }
            };
            CreatePlaylistCommand = new AsyncRelayCommand(async() =>
            {
                var title = _playlistTitle;
                var image = SelectedImage;

                var viewModel = Ioc.Default.GetRequiredService<UserManagerViewModel>();
                UserViewModelBase user = viewModel.SelectedUser;
                if (user != null)
                {
                   var addedPlaylist = 
                       user.AddPlaylist(title, image, user.User);

                    NavigationState.Instance.HomeScreenNavigation.To(addedPlaylist);
                    return;
                }

                NavigationState.Instance.HomeScreenNavigation.Back();
            });
        }


        public AsyncRelayCommand CreatePlaylistCommand { get; }
        public ServiceTypeCheckedHolder[] SyncWithServices { get; }

        public override string Title { get; protected set; }
        public string SelectedImage { get; set; }
    }

    [INotifyPropertyChanged]
    public partial class ServiceTypeCheckedHolder
    {
        [ObservableProperty] private bool _selected = false;
        public ServiceType Service { get; init; }
    }
}

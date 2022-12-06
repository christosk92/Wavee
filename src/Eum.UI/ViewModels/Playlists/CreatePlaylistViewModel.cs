using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Items;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Users;
using Eum.Users;

namespace Eum.UI.ViewModels.Playlists
{
    [INotifyPropertyChanged]
    public partial class CreatePlaylistViewModel : INavigatable
    {
        [ObservableProperty]
        private string? _playlistTitle;
        [ObservableProperty]
        private string? _browsedPicturePath;

        private readonly EumUserViewModel user;
        public CreatePlaylistViewModel(IEumUserViewModelManager userViewModelManager)
        {
            user = userViewModelManager.CurrentUser;

            SyncWithServices = new ServiceTypeCheckedHolder[]
            {
                new ServiceTypeCheckedHolder
                {
                    Service = ServiceType.Spotify,
                    Deselectable = user.User.Id.Service != ServiceType.Spotify,
                    Selected = user.User.Id.Service == ServiceType.Spotify
                }
            };
            CreatePlaylistCommand = new AsyncRelayCommand(async () =>
            {
                var title = _playlistTitle;
                var image = SelectedImage;

                if (user != null)
                {
                    try
                    {
                        var addedPlaylist =
                            await user.CreatePlaylist(title, image, SyncWithServices.Where(a => a.Selected)
                                .Select(a => a.Service).ToArray());

                        NavigationService.Instance.To(addedPlaylist);
                    }
                    catch (Exception x)
                    {

                    }

                    return;
                }

                NavigationService.Instance.GoBack();
            });
        }

        public bool CanSyncWithServices
        {
            get
            {
                //TODO: Linked users
                return user.User.Id.Service != ServiceType.Local;
            }
        }

        public AsyncRelayCommand CreatePlaylistCommand { get; }
        public ServiceTypeCheckedHolder[] SyncWithServices { get; }

        public string Title { get; protected set; }
        public string SelectedImage { get; set; }
        public void OnNavigatedTo(object parameter)
        {
        }

        public void OnNavigatedFrom()
        {
        }

        public int MaxDepth => 0;
    }

    [INotifyPropertyChanged]
    public partial class ServiceTypeCheckedHolder
    {
        [ObservableProperty] private bool _selected = false;
        public ServiceType Service { get; init; }
        public bool Deselectable { get; init; }
    }
}

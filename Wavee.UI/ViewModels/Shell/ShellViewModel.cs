using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Wavee.Spotify.Id;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Navigation;
using Wavee.UI.Utils;
using Wavee.UI.ViewModels.ForYou.Home;
using Wavee.UI.ViewModels.ForYou.Recommended;
using Wavee.UI.ViewModels.Identity.User;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Playback;

namespace Wavee.UI.ViewModels.Shell
{
    public partial class ShellViewModel : ObservableRecipient,
        INavigatable,
        IRecipient<LoggedOutUserChangedMessage>
    {
        [ObservableProperty]
        private bool _isPaneOpen = true;

        [ObservableProperty]
        private SidebarItemViewModel? _selectedSidebarItem;

        public WaveeUserViewModel UserViewModel
        {
            get;
        }

        public PlayerViewModel PlayerViewModel
        {
            get;
        }

        public ShellViewModel(UserManagerViewModel vm, ILoggerFactory? loggerFactory)
        {
            UserViewModel = vm.CurrentUserVal!;
            PlayerViewModel = new PlayerViewModel(vm.CurrentUserVal.User.ServiceType, vm.CurrentUserVal, loggerFactory?.CreateLogger<PlayerViewModel>());
            SidebarItems = new ObservableCollection<SidebarItemViewModel>
            {
                new SidebarHeader("Feed"),
                new HomeViewModelFactory
                {
                    ForService = UserViewModel.User.ServiceType
                },
                new RecommendedViewModelFactory
                {
                    ForService = UserViewModel.User.ServiceType,
                    IsEnabled = true
                },
                new SidebarHeader("Yours"),
                new LibraryViewModelFactory { Type = AudioItemType.Track },
                new LibraryViewModelFactory { Type = AudioItemType.Album },
                new LibraryViewModelFactory { Type = AudioItemType.Artist },
                new LibraryViewModelFactory { Type = AudioItemType.Show },
                new SidebarHeader("Playlists")
            };
            SelectedSidebarItem = SidebarItems[1];
            this.IsActive = true;
        }

        public ObservableCollection<SidebarItemViewModel> SidebarItems
        {
            get;
        }


        // public double OpenPaneLength
        // {
        //     get => _openPaneLength;
        //     set => SetProperty(ref _openPaneLength, value);
        // }


        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            //TODO: save updated properties to disk/user file.
        }

        public void OnNavigatedTo(object parameter)
        {

        }

        public void OnNavigatedFrom()
        {
            WeakReferenceMessenger.Default.Unregister<LoggedInUserChangedMessage>(this);
        }

        public int MaxDepth
        {
            get;
        }

        public void Receive(LoggedOutUserChangedMessage message)
        {
            throw new NotImplementedException();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Wavee.Spotify.Id;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Identity.Users;
using Wavee.UI.Navigation;
using Wavee.UI.Utils;
using Wavee.UI.ViewModels.ForYou;
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
        private double _openPaneLength = 200;

        public WaveeUserViewModel UserViewModel { get; }

        public PlayerViewModel PlayerViewModel { get; }

        public ShellViewModel(UserManagerViewModel vm, IUiDispatcher uiDispatcher)
        {
            UserViewModel = vm.CurrentUserVal!;
            PlayerViewModel = new PlayerViewModel(vm.CurrentUserVal.User.ServiceType, uiDispatcher);
            SidebarItems = new ObservableCollection<SidebarItemViewModel>
            {
                new SidebarHeader("Feed"),
                new HomeViewModelFactory(),
                new RecommendedViewModelFactory
                {
                    ForService = UserViewModel.User.ServiceType,
                },
                new SidebarHeader("Yours"),
                new LibraryViewModelFactory { Type = AudioItemType.Track },
                new LibraryViewModelFactory { Type = AudioItemType.Album },
                new LibraryViewModelFactory { Type = AudioItemType.Artist },
                new LibraryViewModelFactory { Type = AudioItemType.Show },
                new SidebarHeader("Playlists")
            };
            SelectedSidebarItem = SidebarItems[1];
        }

        public ObservableCollection<SidebarItemViewModel> SidebarItems { get; }


        public double OpenPaneLength
        {
            get => _openPaneLength;
            set => SetProperty(ref _openPaneLength, value);
        }


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

        public int MaxDepth { get; }

        public void Receive(LoggedOutUserChangedMessage message)
        {
            throw new NotImplementedException();
        }
    }
}

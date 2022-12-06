using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.Services.Login;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Playback;
using ReactiveUI;

namespace Eum.UI.ViewModels
{
    [INotifyPropertyChanged]
    public partial class MainViewModel
    {
        [ObservableProperty] 
        private PlaybackViewModel? _playbackViewModel;
        private EumUserViewModel _currentUser;

        public MainViewModel(IEumUserViewModelManager userViewModelManager)
        {
            UserViewModelManager = userViewModelManager;
            MainScreen = new NavigationService();
       
            Observable
                .FromEventPattern<EumUserViewModel>(userViewModelManager, nameof(IEumUserViewModelManager.CurrentUserChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.EventArgs as EumUserViewModel)
                .Subscribe(user =>
                {
                    if (PlaybackViewModel != null)
                    {
                        PlaybackViewModel.Deconstruct();
                    }
                    CurrentUser = user;
                    PlaybackViewModel = user.User.Id.Service switch
                    {
                        ServiceType.Spotify => Ioc.Default.GetRequiredService<IEnumerable<PlaybackViewModel>>().FirstOrDefault(a=> a.Service == ServiceType.Spotify)
                    };
                });
        }
        public NavigationService MainScreen { get; }
        public IEumUserViewModelManager UserViewModelManager { get; }

        public EumUserViewModel CurrentUser
        {
            get => _currentUser;
            set => this.SetProperty(ref _currentUser, value);
        }
    }
}

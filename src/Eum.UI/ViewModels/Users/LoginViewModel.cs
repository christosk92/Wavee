using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.Connections.Spotify.Exceptions;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.Services.Login;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels.Navigation;
using Nito.AsyncEx;

namespace Eum.UI.ViewModels.Users
{
    [INotifyPropertyChanged]
    public partial class LoginViewModel
    {
        [ObservableProperty]
        private string? _fatalLoginError;
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasAnyUser;

        [ObservableProperty]
        private ISignInToXViewModel? _signinViewModel = new EmptyViewModel();
        public LoginViewModel(IEumUserViewModelManager usersViewModelManager,
            IAvailableServicesProvider availableServicesProvider)
        {
            UsersViewModelManager = usersViewModelManager;
            AvailableServices = availableServicesProvider.AvailableServices;
            //Check if there are any saved credentials/local profiles/default profiles
            //If there are, load them into the viewmodel
            //If not, show the login screen
            _hasAnyUser = UsersViewModelManager.Users.Any();
        }
        public ServiceType[] AvailableServices { get; }

        public IEumUserViewModelManager UsersViewModelManager { get; }

        public async Task Login(EumUserViewModel user, CancellationToken cancellationToken)
        {
            IsBusy = true;
            try
            {
                await user.LoginAsync(cancellationToken);

                IdentityService.Instance.LoginUser(user.User);
            }
            catch (SpotifyAuthenticationException authentication)
            {
                S_Log.Instance.LogError(authentication.LoginFailed.ToString());
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ResetViewModel()
        {
            SigninViewModel = new EmptyViewModel();
        }

        [RelayCommand]
        private void AddUser(ServiceType service)
        {
            SigninViewModel = Ioc.Default.GetService<IEnumerable<ISignInToXViewModel>>()
                .First(a => a.Service == service);
        }
    }
}
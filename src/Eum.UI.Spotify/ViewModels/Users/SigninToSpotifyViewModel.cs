using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Exceptions;
using Eum.Logging;
using Eum.Spotify;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.Services.Login;
using Eum.UI.Services.Playlists;
using Eum.UI.Services.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Users;

namespace Eum.UI.Spotify.ViewModels.Users
{
    [INotifyPropertyChanged]
    public sealed partial class SignInToSpotifyViewModel : ISignInToXViewModel
    {
        [ObservableProperty]
        private string? _fatalLoginError;
        [ObservableProperty]
        private bool _isBusy;
        [ObservableProperty]
        private string? _username;
        [ObservableProperty]
        private string? _password;

        private readonly ISpotifyClient _spotifyClient;
        private readonly IEumUserManager _eumUserManager;
        public SignInToSpotifyViewModel(ISpotifyClient spotifyClient, IEumUserManager eumUserManager)
        {
            _spotifyClient = spotifyClient;
            _eumUserManager = eumUserManager;
        }



        public void OnNavigatedTo(bool isInHistory) { }

        public void OnNavigatedFrom(bool isInHistory) { }

        public bool IsActive { get; set; }
        public ServiceType Service => ServiceType.Spotify;

        [RelayCommand]
        private async Task Login(CancellationToken ct = default)
        {
            IsBusy = true;

            try
            {
                var signInUser = await _spotifyClient
                    .AuthenticateAsync(new SpotifyUserPassAuthenticator(_username, _password));

                if (signInUser != null)
                {
                    var user = await _eumUserManager
                         .AddUser(_spotifyClient.PrivateUser.Name, signInUser.Username,
                             _spotifyClient.PrivateUser.Avatar?.FirstOrDefault()?.Url, ServiceType.Spotify,
                             new Dictionary<string, object>
                             {
                                {"privateUser", _spotifyClient.PrivateUser},
                                {"authenticatedUser", signInUser}
                             });
                    IdentityService.Instance.LoginUser(user);
                }
            }
            catch (SpotifyAuthenticationException authenticationException)
            {
                S_Log.Instance.LogError(authenticationException);
                FatalLoginError = authenticationException.LoginFailed.ErrorCode switch
                {
                    ErrorCode.BadCredentials => "Invalid username or password.",
                    ErrorCode.TravelRestriction => "You can only use Spotify for 14 days outside your region.",
                    ErrorCode.PremiumAccountRequired => "Premium account is required.",
                    _ => authenticationException.LoginFailed.ErrorCode.ToString()
                };
                S_Log.Instance.LogError(FatalLoginError);
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                FatalLoginError = $"An unknown error occurred: {x.Message}";
            }

            IsBusy = false;
        }
    }
}

using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using Wavee.Spotify.ApiModels.Me;
using Wavee.Spotify.Authentication;
using Wavee.Spotify.Session;
using Wavee.Spotify.Utils;
using Wavee.UI.Identity;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Identity.Users;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Shell;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Wavee.UI.ViewModels.Identity
{
    public partial class SpotifyCredentialsViewModel : AbsCredentialsViewModel
    {
        private string? _username;
        private string? _password;
        private string _profilePicture = string.Empty;
        private string? _cannonicalUsername;
        private bool _isSigningInWithUserPass;

        private readonly WeakReference<ISpotifySession> _spotifySessionRef;
        private readonly WaveeUserManager _userManager;
        public SpotifyCredentialsViewModel(ISpotifySession session, WaveeUserManagerFactory userManagerFactory)
        {
            _spotifySessionRef = new WeakReference<ISpotifySession>(session);
            _userManager = userManagerFactory.GetManager(ServiceType.Spotify);
        }

        public string? Username
        {
            get => _username;
            set => this.SetProperty(ref _username, value);
        }

        public string? Password
        {
            get => _password;
            set => this.SetProperty(ref _password, value);
        }
        public string? CannonicalUsername
        {
            get => _cannonicalUsername;
            set => this.SetProperty(ref _cannonicalUsername, value);
        }
        public string ProfilePicture
        {
            get => _profilePicture;
            set => this.SetProperty(ref _profilePicture, value);
        }

        [RelayCommand(IncludeCancelCommand = true)]
        public async Task SignInWithUserPass(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(Username))
            {
                FatalLoginError = "Username cannot be empty!";
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                FatalLoginError = "Password cannot be empty!";
                return;
            }

            _isSigningInWithUserPass = true;
            FatalLoginError = string.Empty;
            IsSigninIn = true;
            try
            {
                if (!_spotifySessionRef.TryGetTarget(out var ses))
                {
                    ses = Ioc.Default.GetRequiredService<ISpotifySession>();
                }

                var user = await ses.ConnectAsync(
                    new UserPassAuthentication(() => _username,
                        () => _password), ct);

                var privateUserData = await ses.RequestAsync<SpotifyPrivateUser>(HttpMethod.Get, "/me", ct);
                var metadata = ses.UserAttributes.ToDictionary(a => a.Key, a => a.Value);
                metadata["product"] = "spotify premium";
                metadata["private_user"] = JsonSerializer.Serialize(privateUserData, SystemTextJsonOptions.Options);

                CannonicalUsername = privateUserData.Name;
                ProfilePicture = privateUserData.Avatar.FirstOrDefault().Url;
                _userManager.AddUser(
                    user.Username,
                    privateUserData.Name,
                    ProfilePicture,
                    metadata
                );


                var vm = await WeakReferenceMessenger.Default.Send<RequestViewModelForUser>(new RequestViewModelForUser
                {
                    UserId = user.Username,
                    ForService = ServiceType.Spotify
                });
                IsSignedIn = true;
                IsSigninIn = false;
                await Task.Delay(TimeSpan.FromSeconds(1.2), ct);
                vm.SignIn();
                //  NavigationService.Instance.To<ShellViewModel>();
                //Go to main page
            }
            catch (Exception x)
            {
                FatalLoginError = x.Message;
                IsSignedIn = false;
            }

            IsSigninIn = false;
        }

        [RelayCommand]
        public async Task SignInWithOfflineProfileCommand(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(Username))
            {
                FatalLoginError = "Please enter a name for te offline profile.";
                return;
            }

            var metadata = new Dictionary<string, string>();
            metadata["product"] = "offline";

            CannonicalUsername = Username;
            WaveeUserManager offlineManager;
            offlineManager = Ioc.Default.GetRequiredService<WaveeUserManagerFactory>()
                .GetManager(ServiceType.Local);
            offlineManager.AddUser(
                Username,
                null,
                ProfilePicture,
                metadata
            );

            var vm = await WeakReferenceMessenger.Default.Send<RequestViewModelForUser>(new RequestViewModelForUser
            {
                UserId = Username,
                ForService = ServiceType.Local
            });
            IsSignedIn = true;
            IsSigninIn = false;
            await Task.Delay(TimeSpan.FromSeconds(1.2), ct);
            vm.SignIn();
        }

        [RelayCommand(IncludeCancelCommand = true)]
        public async Task SigninWithStoredCredentials(CancellationToken ct = default)
        {
            _isSigningInWithUserPass = false;
        }

        public override ICommand CancelSignInCommand => _isSigningInWithUserPass ?
            SignInWithUserPassCancelCommand
            : SigninWithStoredCredentialsCancelCommand;
    }
}

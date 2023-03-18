using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;
using Wavee.UI.ViewModels.Login.Impl;

namespace Wavee.UI.ViewModels.Login
{
    public class LoginViewModel : ObservableObject
    {
        private AbsLoginServiceViewModel? _selectedService;
        private bool _isSignedIn = false;

        public LoginViewModel(IProfileManager profileManager)
        {
            if (profileManager.HasDefaultProfile())
            {
                //if we have a default profile, we can skip the login screen
                //and go straight to the main screen
                var profile = profileManager.GetDefaultProfile();
                if (profile!.Value.ServiceType is ServiceType.Spotify)
                {
                    //perform login with stored credentials
                    SelectedService = new StoredCredentialsSpotifyLoginViewModel(profile.Value)
                    {
                        OnSignedIn = OnSignedIn,
                    };
                }
                else if (profile?.ServiceType is ServiceType.Local)
                {
                    //we can skip the login screen and go straight to the main screen
                    SignedInProfile = profile;
                    IsSignedIn = true;
                }

                return;
            }

            //Either we don't have a default profile, or we do not have any profiles at all.
            //Either way, we need to show the user what to do next.
            if (profileManager.HasAnyProfile())
            {
                //we have profiles, but no default profile
                //we need to ask the user which profile they want to use
                SelectedService = new SelectProfileViewModel(profileManager)
                {
                    OnSignedIn = OnSignedIn,
                    OnDifferentServiceRequested = type => SelectedService = BuildService(type)
                };
            }
            else
            {
                //we don't have any profiles at all
                //we need to ask the user to create a profile
                SelectedService = new SpotifyLoginViewModel()
                {
                    OnSignedIn = OnSignedIn,
                    OnDifferentServiceRequested = type => SelectedService = BuildService(type)
                };
            }
        }

        public Profile? SignedInProfile { get; private set; }

        public ServiceType[] AvailableServices => new[]
        {
            ServiceType.Local,
            ServiceType.Spotify
        };

        public AbsLoginServiceViewModel? SelectedService
        {
            get => _selectedService;
            private set => SetProperty(ref _selectedService, value);
        }

        public bool IsSignedIn
        {
            get => _isSignedIn;
            private set => SetProperty(ref _isSignedIn, value);
        }

        private AbsLoginServiceViewModel BuildService(ServiceType? serviceType)
        {
            return serviceType switch
            {
                ServiceType.Local => new CreateLocalProfileViewModel(Ioc.Default.GetRequiredService<IProfileManager>())
                {
                    OnDifferentServiceRequested = (s) => SelectedService = BuildService(s),
                    OnSignedIn = OnSignedIn
                },
                ServiceType.Spotify => new SpotifyLoginViewModel()
                {
                    OnDifferentServiceRequested = (s) => SelectedService = BuildService(s),
                    OnSignedIn = OnSignedIn
                },
                _ => new SelectProfileViewModel(Ioc.Default.GetRequiredService<IProfileManager>())
                {
                    OnDifferentServiceRequested = (s) => SelectedService = BuildService(s),
                    OnSignedIn = OnSignedIn
                },
            };
        }

        private void OnSignedIn(Profile? profile)
        {
            SignedInProfile = profile;
            IsSignedIn = true;
        }
    }
}
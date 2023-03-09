using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Session;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Navigation;

namespace Wavee.UI.ViewModels.Identity
{
    public sealed partial class SignInViewModel : ObservableRecipient, INavigatable, IRecipient<LoggedInUserChangedMessage>

    {
        private AbsCredentialsViewModel? _absCredentialsViewModel;
        private bool _isSignedInOverride;

        public SignInViewModel()
        {
            //TODO: Only Spotify for now
            SelectedService = Ioc.Default.GetServices<AbsCredentialsViewModel>()
            .First();

            WeakReferenceMessenger.Default.Register<LoggedInUserChangedMessage>(this);
        }

        public AbsCredentialsViewModel? SelectedService
        {
            get => _absCredentialsViewModel;
            set
            {
                if(SetProperty(ref _absCredentialsViewModel, value))
                {
                    OnPropertyChanged(nameof(IsLoggedIn));
                }
            }
        }

        public bool IsLoggedIn => _absCredentialsViewModel?.IsSignedIn is true || _isSignedInOverride;
        public void Receive(LoggedInUserChangedMessage message)
        {
            _isSignedInOverride = message.Value != null;
            OnPropertyChanged(nameof(IsLoggedIn));
        }

        public void OnNavigatedTo(object parameter) { }

        public void OnNavigatedFrom()
        {
            WeakReferenceMessenger.Default.Unregister<LoggedInUserChangedMessage>(this);
        }

        public int MaxDepth { get; }
    }
}

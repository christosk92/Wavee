using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Wavee.UI.Identity;
using Wavee.UI.Identity.Messaging;

namespace Wavee.UI.ViewModels.Identity
{
    public abstract class AbsCredentialsViewModel : ObservableRecipient
    {
        private string? _fatalLoginErrror;
        private bool _isSigningIn;
        private bool _isSignedIn;
        public string? FatalLoginError
        {
            get => _fatalLoginErrror;
            protected set => SetProperty(ref _fatalLoginErrror, value);
        }
        public bool IsSigninIn
        {
            get => _isSigningIn;
            protected set => SetProperty(ref _isSigningIn, value);
        }

        public bool IsSignedIn
        {
            get => _isSignedIn;
            protected set => SetProperty(ref _isSignedIn, value);
        }

        public abstract ICommand CancelSignInCommand { get; }
    }
}

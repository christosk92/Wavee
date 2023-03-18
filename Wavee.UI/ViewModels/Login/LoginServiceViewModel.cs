using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.ViewModels.Login;

public abstract partial class AbsLoginServiceViewModel : ObservableObject
{
    private string? _fatalLoginError;
    private bool _isSigningIn;
    public SignedIn? OnSignedIn { get; set; }
    public DifferentServiceRequested? OnDifferentServiceRequested { get; set; }

    public string? FatalLoginError
    {
        get => _fatalLoginError;
        protected set => SetProperty(ref _fatalLoginError, value);
    }

    public bool IsSigningIn
    {
        get => _isSigningIn;
        protected set => SetProperty(ref _isSigningIn, value);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    protected abstract Task SignIn(CancellationToken ct = default);
}

public delegate void SignedIn(Profile? profile);

public delegate void DifferentServiceRequested(ServiceType? serviceType);
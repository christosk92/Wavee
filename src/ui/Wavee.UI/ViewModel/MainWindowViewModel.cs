using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Identity;
using Wavee.UI.ViewModel.Setup;
using Wavee.UI.ViewModel.Wizard;
using Wavee.UI.WinUI.Providers;

namespace Wavee.UI.ViewModel;
public sealed class MainWindowViewModel : ObservableObject
{
    private object _view;
    public MainWindowViewModel(GlobalSettings settings)
    {
        Settings = settings;
    }

    public object CurrentView
    {
        get => _view;
        set => this.SetProperty(ref _view, value);
    }

    public GlobalSettings Settings { get; }

    public async Task Initialize()
    {
        var hasDefaultUser = !string.IsNullOrEmpty(Settings.DefaultUser);
        if (hasDefaultUser)
        {
            var potentialCredentials = AppProviders.GetCredentialsFor(Settings.DefaultUser);
            if (potentialCredentials.IsSome)
            {
                var identity = new IdentityViewModel();
                CurrentView = identity;
                await identity.SignInAsync(Settings.DefaultUser, potentialCredentials.ValueUnsafe());
                return;
            }
        }

        CurrentView = CreateSetupWizard();
    }

    private WizardViewModel CreateSetupWizard()
    {
        //1) Welcome message
        //2) Sign In
        //3) Setting everythign up
        //4) Opt in
        //5 ) you're good to go
        const int totalSteps = 5;

        return new WizardViewModel(
            totalSteps: totalSteps,
            viewModelFactory: SetupWizardFactory);
    }

    private IWizardViewModel SetupWizardFactory(int arg)
    {
        return arg switch
        {
            0 => new WelcomeViewModel(),
            _ => throw new NotImplementedException()
        };
    }
}
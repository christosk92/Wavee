using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Id;
using Wavee.UI.Contracts;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Setup;
using Wavee.UI.ViewModel.Wizard;
using Wavee.UI.WinUI.Providers;

namespace Wavee.UI.ViewModel;
public sealed class MainWindowViewModel : ObservableObject
{
    private object _view;
    private readonly Func<ServiceType, IMusicEnvironment> _environmentFactory;
    public MainWindowViewModel(GlobalSettings settings, Func<ServiceType, IMusicEnvironment> environmentFactory)
    {
        Settings = settings;
        _environmentFactory = environmentFactory;
        Shared.GlobalSettings = settings;
    }

    public object CurrentView
    {
        get => _view;
        set => this.SetProperty(ref _view, value);
    }

    public GlobalSettings Settings { get; }

    public async Task<Option<UserViewModel>> Initialize()
    {
        var hasDefaultUser = !string.IsNullOrEmpty(Settings.DefaultUser);
        if (hasDefaultUser)
        {
            var identity = new IdentityViewModel(_environmentFactory);
            CurrentView = identity;
            return await identity.SignInAsync(Settings.DefaultUser);
        }

        CurrentView = CreateSetupWizard();
        return Option<UserViewModel>.None;
    }

    private WizardViewModel CreateSetupWizard()
    {
        //1) Welcome message
        //2) Sign In
        //3) Setting everything up up
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
            1 => new IdentityViewModel(_environmentFactory),
            2 => new SettingEverythingUpViewModel(_environmentFactory),
        };
    }
}
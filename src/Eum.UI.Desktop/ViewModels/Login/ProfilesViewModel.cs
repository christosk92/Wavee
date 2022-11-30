using System.Reactive.Concurrency;
using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using Eum.UI.Helpers;
using Eum.UI.Stage;
using Eum.UI.Users;
using Eum.UI.ViewModels.Profile.Create;
using Eum.UI.ViewModels.Users;
using Eum.Users;
using Newtonsoft.Json;
using ReactiveUI;

namespace Eum.UI.ViewModels.Login;

public partial class ProfilesViewModel : ViewModelBase
{
    private bool _isUserListVisible;
    private bool _hasUser;
    private bool _showProfiles = true;
    private UserViewModelBase _selectedProfileForSignIn;
    private bool _isBusy;

    public ProfilesViewModel()
    {
        CreateOrLinkProfileViewModel = new CreateOrLinkProfileViewModel(this);
        UserManager = Ioc.Default.GetRequiredService<UserManagerViewModel>();
        HasUser = UserManager.Users.Count > 0;
        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            if (Ioc.Default.GetRequiredService<UserManager>().HasUser())
            {
                IsUserListVisible = true;
            }
        });

        CreateProfileCommand = new RelayCommand<int>(CreateProfile);
        DeleteProfileCommand = new RelayCommand<UserViewModelBase>(user =>
        {
            var userManager = Ioc.Default.GetRequiredService<UserManager>();
            userManager.RemoveUser(user.User);

            HasUser = userManager.HasUser();
        });
        this.WhenAnyValue(a => a.SelectedProfileForSignIn)
            .Subscribe(async a =>
            {
                if (a != null)
                {
                    ShowProfiles = false;
                    await SignInTask(a);
                }
                else
                {
                    ShowProfiles = true;
                }
            });
    }

    public ICommand DeleteProfileCommand { get; }

    public UserManagerViewModel UserManager { get; }
    public CreateOrLinkProfileViewModel CreateOrLinkProfileViewModel { get; }
    public bool IsUserListVisible
    {
        get => _isUserListVisible;
        set => this.RaiseAndSetIfChanged(ref _isUserListVisible, value);
    }

    public bool HasUser
    {
        get => _hasUser;
        set => this.RaiseAndSetIfChanged(ref _hasUser, value);
    }
    public ICommand CreateProfileCommand { get; }
    public bool ShowProfiles
    {
        get => _showProfiles;
        set => this.RaiseAndSetIfChanged(ref _showProfiles, value);
    }

    public UserViewModelBase SelectedProfileForSignIn
    {
        get => _selectedProfileForSignIn;
        set => this.RaiseAndSetIfChanged(ref _selectedProfileForSignIn, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    

    public async void CreateProfile(int serviceType)
    {
        if (serviceType == 2)
        {
            IsUserListVisible = true;
            Ioc.Default.GetRequiredService<MainViewModel>().IsLoginVisible = true;
            return;
        }
        switch ((ServiceType)serviceType)
        {
            case ServiceType.Spotify:
                IsUserListVisible = false;
                break;
            case ServiceType.Local:
                //show a dialog.
                var stageManager = new StageManager(new SetupProfile_StepOneViewModel(), 4);
                var profile = await Ioc.Default.GetRequiredService<IDialogHelper>()
                    .ShowStageManagerAsDialogAsync(stageManager);
                HasUser = Ioc.Default.GetRequiredService<UserManager>().HasUser();
                break;
        }
    }

    private async Task SignInTask(UserViewModelBase user, string? pwd = null)
    {
        try
        {
            IsBusy = true;
            user.User.IsLoggedIn = true;
            UserManager.SetSelectedUser(user);
        }
        catch (Exception x)
        {

        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class PictureItem
{
    [JsonPropertyName("src")]
    public string? Src { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
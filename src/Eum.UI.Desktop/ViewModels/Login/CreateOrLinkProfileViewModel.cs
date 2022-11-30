using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Users;
using Eum.Users;
using ReactiveUI;

namespace Eum.UI.ViewModels.Login;

public partial class CreateOrLinkProfileViewModel : ViewModelBase
{
    [AutoNotify] private bool _isBusy;
    [AutoNotify] private string? _password;
    [AutoNotify] private string? _username;
    private readonly ProfilesViewModel _profilesViewModel;

    public CreateOrLinkProfileViewModel(ProfilesViewModel profilesViewModel)
    {
        _profilesViewModel = profilesViewModel;
        ShowProfilesListCommand = ReactiveCommand.Create(() =>
        {
            _profilesViewModel.IsUserListVisible = true;
        });

        LoginCommand = new AsyncRelayCommand<int>(LoginToServiceTask);
    }

    private async Task LoginToServiceTask(int serviceType)
    {
        switch ((ServiceType)serviceType)
        {
            case ServiceType.Spotify:
                _profilesViewModel.IsUserListVisible = false;
                IsBusy = true;
                await Task.Delay(TimeSpan.FromSeconds(3));
                IsBusy = false;
                break;
            case ServiceType.Local:
                _profilesViewModel.IsUserListVisible = true;
                break;
        }
    }

    public ICommand ShowProfilesListCommand { get; }
    public ICommand LoginCommand { get; }
}
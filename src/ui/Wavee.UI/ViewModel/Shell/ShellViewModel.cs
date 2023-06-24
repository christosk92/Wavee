using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Shell;

public sealed class ShellViewModel : ObservableObject
{
    public ShellViewModel(UserViewModel user)
    {
        User = user;
    }
    public UserViewModel User { get; set; }
}
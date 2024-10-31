using ReactiveUI;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.ViewModels.Navigation;

namespace Wavee.ViewModels.ViewModels.Users;

[AppLifetime]
public partial class UserViewModel : RoutableViewModel
{
    [AutoNotify] private bool _isSelected;

    private string _title = "";

    public UserViewModel(
        UiContext uiContext,
        UserModel userModel,
        User user)
    {
        UiContext = uiContext;
        UserModel = userModel;
        User = user;
    }

    // TODO: Remove this
    public User User { get; }
    public UserModel UserModel { get; }

    public override string Title
    {
        get => _title;
        protected set => this.RaiseAndSetIfChanged(ref _title, value);
    }
}

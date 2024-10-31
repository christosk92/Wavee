using System.Reactive.Disposables;
using ReactiveUI;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.ViewModels.Navigation;

namespace Wavee.ViewModels.ViewModels.Users;

public partial class UserPageViewModel : ViewModelBase
{
    private readonly CompositeDisposable _disposables = new();

    [AutoNotify] private bool _isLoggedIn;
    [AutoNotify] private bool _isSelected;
    [AutoNotify] private bool _isLoading;
    [AutoNotify] private RoutableViewModel? _currentPage;
    [AutoNotify] private UserViewModel? _userViewModel;
    private UserPageViewModel(UserModel userModel)
    {
        UserModel = userModel;

        // TODO: Finish partial refactor
        // User property must be removed
        User = Services.UserManager.GetUserById(userModel.Id)
               ?? throw new NotSupportedException();
    }

    public UserModel UserModel { get; }
    public User User { get; }

    private void ShowUser()
    {
        UserViewModel = new UserViewModel(UiContext, UserModel, User);

        // Pass IsSelected down to UserViewModel.IsSelected
        this.WhenAnyValue(x => x.IsSelected)
            .BindTo(UserViewModel, x => x.IsSelected)
            .DisposeWith(_disposables);

        CurrentPage = UserViewModel;
        IsLoading = false;
    }
}
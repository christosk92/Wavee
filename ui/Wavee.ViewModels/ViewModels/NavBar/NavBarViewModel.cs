using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.Navigation;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.ViewModels.Navigation;
using Wavee.ViewModels.ViewModels.Users;

namespace Wavee.ViewModels.ViewModels.NavBar;

/// <summary>
/// The ViewModel that represents the structure of the sidebar.
/// </summary>
[AppLifetime]
public partial class NavBarViewModel : ViewModelBase, IUserNavigation
{
    [AutoNotify] private UserPageViewModel? _selectedUser;

    [AutoNotify] private UserModel? _selectedUserModel;
    public NavBarViewModel(UiContext uiContext)
    {
        UiContext = uiContext;

        BottomItems = new ObservableCollection<NavBarItemViewModel>();

        UiContext.UserRepository
            .Users
            .Connect()
            .Transform(newUser => new UserPageViewModel(UiContext, newUser))
            .AutoRefresh(x => x.IsLoggedIn)
            .SortAndBind(
                out var users,
               SortExpressionComparer<UserPageViewModel>
                .Descending(i => i.IsLoggedIn)
                .ThenByAscending(x => x.UserModel.Name))
            .Subscribe();

        Users = users;
    }
    public ObservableCollection<NavBarItemViewModel> BottomItems { get; }

    public ReadOnlyObservableCollection<UserPageViewModel> Users { get; }

    public UserViewModel? To(UserModel user)
    {
        SelectedUser = Users.First(x => x.UserModel.Id== user.Id);
        return SelectedUser.UserViewModel;
    }

    public void Activate()
    {
        this.WhenAnyValue(x => x.SelectedUser)
            .Buffer(2, 1)
            .Select(buffer => (OldValue: buffer[0], NewValue: buffer[1]))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x =>
            {
                if (x.OldValue is { } a)
                {
                    a.IsSelected = false;
                }

                if (x.NewValue is { } b)
                {
                    b.IsSelected = true;
                    UiContext.UserRepository.StoreLastSelectedUser(b.UserModel);
                }
            })
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedUser!.UserModel)
            .BindTo(this, x => x.SelectedUserModel);

        SelectedUser = Users.FirstOrDefault(x => x.UserModel.Id == UiContext.UserRepository.DefaultUserId) ?? Users.FirstOrDefault();
    }

    public async Task InitialiseAsync()
    {
        var bottomItems = NavigationManager
            .MetaData
            .Where(x => x.NavBarPosition == NavBarPosition.Bottom);

        foreach (var item in bottomItems)
        {
            var viewModel = await NavigationManager.MaterializeViewModelAsync(item);

            if (viewModel is INavBarItem navBarItem)
            {
                BottomItems.Add(new NavBarItemViewModel(navBarItem));
            }
        }
    }
}
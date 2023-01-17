using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Eum.UI.Services.Login;
using Eum.UI.Services.Playlists;
using Eum.UI.Users;
using Eum.Users;
using Nito.AsyncEx;
using ReactiveUI;

namespace Eum.UI.Services.Users
{
    [INotifyPropertyChanged]
    public sealed partial class EumUserViewModelManager : IEumUserViewModelManager
    {
        private readonly SourceList<EumUserViewModel> _usersSourceList = new();
        private readonly ObservableCollectionExtended<EumUserViewModel> _users = new();
        private readonly ObservableCollectionExtended<EumUserViewModel> _canLoginUsers = new();

        [ObservableProperty]
        private EumUserViewModel? _currentUser;
        private readonly IEumUserManager _userManager;
        public EumUserViewModelManager(IEumUserManager userManager)
        {
            _userManager = userManager;
            _usersSourceList
                .Connect()
                .Sort(SortExpressionComparer<EumUserViewModel>
                    .Descending(i => i.User.IsDefault))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_users)
                .Subscribe();

            _usersSourceList
                .Connect()
                .Sort(SortExpressionComparer<EumUserViewModel>
                    .Descending(i => i.User.IsDefault))
                .Filter(a=> a.User.Metadata?.ContainsKey("authenticatedUser") ?? false)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_canLoginUsers)
                .Subscribe();

            Observable
                .FromEventPattern<EumUser>(IdentityService.Instance, nameof(IdentityService.UserLoggedIn))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(async x =>
                {
                    var user = x.EventArgs;
                    if (user == null)
                    {
                        CurrentUser = null;
                        CurrentUserChanged?.Invoke(this, null);
                        return;
                    }
                    EumUserViewModel userViewModel;
                    while (!TryGetUserViewModel(user, out userViewModel))
                    {
                        await Task.Delay(10);
                    }

                    CurrentUser = userViewModel;

                    if (user.IsDefault)
                    {
                        foreach (var eumUser in await _userManager.GetUsers(false))
                        {
                            if (eumUser.Id != user.Id)
                            {
                                eumUser.IsDefault = false;
                            }
                        }
                    }
                    _ = Task.Run(async () => await userViewModel.Sync());
                    CurrentUserChanged?.Invoke(this, userViewModel);
                })
                .Subscribe();


            Observable
                .FromEventPattern<EumUser>(_userManager, nameof(IEumUserManager.UserAdded))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(user =>
                {
                    var vm = EumUserViewModel.Create(user);
                    _usersSourceList.Add(vm);
                });
            Observable
                .FromEventPattern<EumUser>(_userManager, nameof(IEumUserManager.UserUpdated))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(user =>
                {
                    if (TryGetUserViewModel(user, out var toUpdate))
                    {
                        toUpdate.User = user;
                        _ = Task.Run(async () => await toUpdate.Sync());
                    }
                });
            Observable
                .FromEventPattern<EumUser>(_userManager, nameof(IEumUserManager.UserRemoved))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(wallet =>
                {
                    if (TryGetUserViewModel(wallet, out var toRemoveUser))
                    {
                        _usersSourceList.Remove(toRemoveUser);
                    }
                });
            AsyncContext.Run(async() => await EnumerateUsers());
        }

        public ObservableCollection<EumUserViewModel> CanLoginUsers => _canLoginUsers;
        public ObservableCollection<EumUserViewModel> Users => _users;
        public event EventHandler<EumUserViewModel> CurrentUserChanged; 
        private bool TryGetUserViewModel(EumUser user,  out EumUserViewModel? userViewModel)
        {
            userViewModel = Users.FirstOrDefault(x => x.User.Id == user.Id);
            return userViewModel is { };
        }


        private async Task EnumerateUsers()
        {
            foreach (var user in await _userManager.GetUsers(true))
            {
                var newUser = EumUserViewModel.Create(user);
                _usersSourceList.Add(newUser);
            }
        }
    }
}

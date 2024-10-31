using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.ViewModels.Extensions;
using Wavee.ViewModels.Interfaces;

namespace Wavee.ViewModels.Models.Users;

public partial class UserRepository : ReactiveObject, IUserRepository
{
    private readonly Dictionary<string, UserModel> _userDictionary = new();
    private readonly CompositeDisposable _disposable = new();

    public UserRepository()
    {
        var signals =
            Observable.FromEventPattern<User>(Services.UserManager, nameof(UserManager.UserAdded))
                .Select(_ => System.Reactive.Unit.Default)
                .StartWith(System.Reactive.Unit.Default);

        Users = signals
                .Fetch(() => Services.UserManager.GetUsers(), x => x.Id)
                .DisposeWith(_disposable)
                .Connect()
                .TransformWithInlineUpdate(CreateUserModel, (_, _) => { })
                .AsObservableCache()
                .DisposeWith(_disposable);

        DefaultUserId = Services.UiConfig.LastSelectedUser;
    }
    public IObservableCache<UserModel, string> Users { get; }
    public string? DefaultUserId { get; }
    public bool HasUser => Services.UserManager.HasUser();

    public void StoreLastSelectedUser(UserModel user)
    {
        Services.UiConfig.LastSelectedUser = user.Id;
    }

    private UserModel CreateUserModel(User user)
    {
        if (_userDictionary.TryGetValue(user.Id, out var existing))
        {
            if (!ReferenceEquals(existing.User, user))
            {
                throw new InvalidOperationException($"Different instance of: {user.Id}");
            }
            return existing;
        }

        var result = new UserModel(user);

        _userDictionary[user.Id] = result;

        return result;
    }
}
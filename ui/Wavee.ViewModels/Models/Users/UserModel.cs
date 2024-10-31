using ReactiveUI;
using Wavee.ViewModels.Infrastructure;

namespace Wavee.ViewModels.Models.Users;

[AppLifetime]
public partial class UserModel : ReactiveObject
{
    [AutoNotify] private bool _isLoggedIn;
    [AutoNotify] private bool _isLoaded;
    [AutoNotify] private bool _isSelected;

    public UserModel(User user)
    {
        User = user;
    }

    internal User User { get; }
    public string Id => User.Id;
    public string Name => User.Name;
}
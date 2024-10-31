using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.ViewModels.Users;

namespace Wavee.ViewModels.ViewModels.Navigation;

public interface IUserNavigation
{
    UserViewModel? To(UserModel user);
}

using Eum.UI.Services;
using Eum.UI.Users;

namespace Eum.UI.WinUI;

public class WinUiThemeSelectorServiceFactory : IThemeSelectorServiceFactory
{
    public IThemeSelectorService GetThemeSelectorService(EumUser forUser)
    {
        return new WinUiUserThemeSelectorService(forUser);
    }
}
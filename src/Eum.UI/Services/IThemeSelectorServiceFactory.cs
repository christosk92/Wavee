using System;
using System.Collections.Generic;
using System.Text;
using Eum.UI.Users;

namespace Eum.UI.Services
{
    public interface IThemeSelectorServiceFactory
    {
        IThemeSelectorService GetThemeSelectorService(EumUser forUser);
    }
}

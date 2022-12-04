using System.Collections.ObjectModel;
using Eum.UI.Users;

namespace Eum.UI.Services.Users
{
    public interface IEumUserViewModelManager
    {
        EumUserViewModel? CurrentUser { get; }
        ObservableCollection<EumUserViewModel> CanLoginUsers { get; }
        ObservableCollection<EumUserViewModel> Users { get; }
        event EventHandler<EumUserViewModel> CurrentUserChanged;
    }
}

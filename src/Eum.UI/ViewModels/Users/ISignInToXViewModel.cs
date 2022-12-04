using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Items;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI.ViewModels.Users
{
    public interface ISignInToXViewModel : INavigatable
    {
        ServiceType Service { get; }
        string? FatalLoginError { get; set; }
    }
}

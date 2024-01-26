using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.Services;

public interface INavigationController : IDisposable
{
    void NavigateTo(object viewmodel);
}
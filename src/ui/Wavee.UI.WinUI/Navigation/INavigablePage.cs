using Microsoft.UI.Xaml.Navigation;

namespace Wavee.UI.WinUI.Navigation;
public interface INavigable
{
    void NavigatedTo(object parameter);
    void NavigatedFrom(NavigationMode mode);
}
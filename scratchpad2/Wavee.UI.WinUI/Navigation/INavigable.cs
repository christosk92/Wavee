
using System;
using Microsoft.UI.Xaml.Navigation;

namespace Wavee.UI.Navigation;
public interface INavigable
{
    void NavigatedTo(object parameter);
    void NavigatedFrom(NavigationMode mode);
}

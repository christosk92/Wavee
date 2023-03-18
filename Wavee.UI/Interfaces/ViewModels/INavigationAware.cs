namespace Wavee.UI.Interfaces.ViewModels;

public interface INavigationAware
{
    void OnNavigatedTo(object parameter);

    void OnNavigatedFrom();
}

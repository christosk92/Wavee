namespace Wavee.UI.WinUI.Navigation;

public interface INavigationAwareView
{
    /// <summary>
    /// Invoked when the page is navigated to.
    /// This method is always invoked when the page is navigated to, regardless of whether the page is being cached or not.
    /// </summary>
    /// <param name="parameter"></param>
    void OnNavigatedTo(object? parameter);
}
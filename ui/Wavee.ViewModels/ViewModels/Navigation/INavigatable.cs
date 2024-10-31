namespace Wavee.ViewModels.ViewModels.Navigation;

public interface INavigatable
{
    void OnNavigatedTo(bool isInHistory);

    void OnNavigatedFrom(bool isInHistory);
}
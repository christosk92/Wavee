namespace Eum.UI.ViewModels.Navigation;

public interface INavigatable
{
    void OnNavigatedTo(bool isInHistory);

    void OnNavigatedFrom(bool isInHistory);

    bool IsActive { get; set; }
}
namespace Wavee.UI.Navigation;

public interface INavigatable
{
    void OnNavigatedTo(object parameter);

    void OnNavigatedFrom();
    int MaxDepth { get; }
    
}
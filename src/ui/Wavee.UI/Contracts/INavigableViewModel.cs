namespace Wavee.UI.Contracts
{
    public interface INavigableViewModel
    {
        void OnNavigatedTo(object? parameter);
        void OnNavigatedFrom();
    }
}

namespace Wavee.UI.WinUI.Contracts;

public interface INavigeablePage<T> : INavigeablePageNonGeneric
{
    T ViewModel { get; }
}

public interface INavigeablePageNonGeneric
{
    void UpdateBindings();
}
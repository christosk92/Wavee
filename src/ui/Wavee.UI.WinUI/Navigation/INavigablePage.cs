using Wavee.UI.Contracts;

namespace Wavee.UI.WinUI.Navigation;
public interface INavigablePage
{
    bool ShouldKeepInCache(int depth);
    INavigableViewModel ViewModel { get; }
}
using LanguageExt;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI;
public interface INavigablePage
{
    bool ShouldKeepInCache(int depth);
    Option<INavigableViewModel> ViewModel { get; }
    void NavigatedTo(object? parameter);
    void RemovedFromCache();
}
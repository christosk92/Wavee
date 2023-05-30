namespace Wavee.UI.WinUI.Navigation;

public interface ICacheablePage
{
    bool ShouldCache(int depth);
    void RemovedFromCache();
}
namespace Wavee.UI.WinUI.Navigation;

public interface ICacheablePage
{
    bool ShouldKeepInCache(int currentDepth);
    void RemovedFromCache();
}
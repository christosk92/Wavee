using Eum.UI.ViewModels.Settings;

namespace Eum.UI.ViewModels.Navigation
{
    public interface IGlazeablePage
    {
        bool ShouldSetPageGlaze { get; }
        ValueTask<string> GetGlazeColor(AppTheme theme, CancellationToken ct = default);
    }
}

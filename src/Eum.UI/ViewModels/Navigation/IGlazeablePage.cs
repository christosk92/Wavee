namespace Eum.UI.ViewModels.Navigation
{
    public interface IGlazeablePage
    {
        bool ShouldSetPageGlaze { get; }
        ValueTask<string> GetGlazeColor(CancellationToken ct = default);
    }
}

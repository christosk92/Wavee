using Wavee.UI.Common;

namespace Wavee.UI.ViewModel.Home;

public sealed class HomeGroupSectionViewModel
{
    public IReadOnlyCollection<ICardViewModel> Items { get; init; }
    public string Title { get; init; }
    public string? Subtitle { get; init; }
    public HomeGroupRenderType Rendering { get; init; }
}

public enum HomeGroupRenderType
{
    HorizontalStack,
    Grid
}
using ReactiveUI;

namespace Wavee.UI.ViewModels.Sidebar;

public class CountedSidebarItem : RegularSidebarItem
{
    private int _count;
    public required int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }
}
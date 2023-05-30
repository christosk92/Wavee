using ReactiveUI;

namespace Wavee.UI.WinUI.Views.Sidebar.Items;

public abstract class AbsSidebarItemViewModel : ReactiveObject
{
    public required string Title { get; set; }
}
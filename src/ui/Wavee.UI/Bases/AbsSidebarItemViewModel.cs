using ReactiveUI;

namespace Wavee.UI.Bases;

public abstract class AbsSidebarItemViewModel : ReactiveObject
{
    public required string Title { get; set; }
}
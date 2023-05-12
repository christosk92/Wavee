using Wavee.UI.Bases;

namespace Wavee.UI.ViewModels.Sidebar;

public class RegularSidebarItem : AbsSidebarItemViewModel
{
    public required string Icon { get; init; }
    public required string IconFontFamily { get; init; }
}
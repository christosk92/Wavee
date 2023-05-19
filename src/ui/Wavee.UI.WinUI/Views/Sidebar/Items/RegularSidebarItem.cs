namespace Wavee.UI.WinUI.Views.Sidebar.Items;

public class RegularSidebarItem : AbsSidebarItemViewModel
{
    public required string Slug { get; init; }
    public required string Icon { get; init; }
    public required string IconFontFamily { get; init; }
}
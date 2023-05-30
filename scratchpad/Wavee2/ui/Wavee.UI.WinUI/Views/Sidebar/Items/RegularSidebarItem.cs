using System;

namespace Wavee.UI.WinUI.Views.Sidebar.Items;

public class RegularSidebarItem : AbsSidebarItemViewModel
{
    public required string Icon { get; init; }
    public required string IconFontFamily { get; init; }
    public bool ForceDisable { get; init; }
    public required Type ViewType { get; init; }
}
namespace Wavee.UI.ViewModels.Shell.Sidebar;

public readonly record struct SidebarItemHeader(string Content, Type? NavigateTo = null) : ISidebarItem;
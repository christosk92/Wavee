namespace Wavee.UI.ViewModels.Shell.Sidebar;

public record GenericSidebarItem(string Id, string Content, string Icon, Type NavigateTo) : ISidebarItem;
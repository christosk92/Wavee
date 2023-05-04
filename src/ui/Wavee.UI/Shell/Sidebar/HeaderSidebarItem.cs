using LanguageExt;

namespace Wavee.UI.Shell.Sidebar;

public readonly record struct HeaderSidebarItem(string Id, string Content) : ISidebarItem;
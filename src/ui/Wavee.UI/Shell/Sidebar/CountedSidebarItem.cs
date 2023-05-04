using LanguageExt;

namespace Wavee.UI.Shell.Sidebar;

public readonly record struct CountedSidebarItem(
    string Id,
    string Content,
    string Icon,
    Option<string> IconFontFamily, 
    int Count) : ISidebarItem;
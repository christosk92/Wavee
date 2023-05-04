using LanguageExt;

namespace Wavee.UI.Shell.Sidebar;

public readonly record struct GeneralSidebarItem(
    string Id,
    string Content,
    string Icon,
    Option<string> IconFontFamily) : ISidebarItem;
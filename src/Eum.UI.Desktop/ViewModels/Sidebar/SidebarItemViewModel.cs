using CommunityToolkit.Mvvm.ComponentModel;
using Eum.UI.ViewModels.NavBar;

namespace Eum.UI.ViewModels.Sidebar;
public abstract partial class SidebarItemViewModel : NavBarItemViewModel, ISidebarItem
{
    [AutoNotify]
    private bool _isSelected;
    public abstract string Glyph { get; }

    public virtual string GlyphFontFamily { get; } = "Segoe Fluent Icons";
}
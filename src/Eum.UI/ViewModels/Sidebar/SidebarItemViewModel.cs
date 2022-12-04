using CommunityToolkit.Mvvm.ComponentModel;

namespace Eum.UI.ViewModels.Sidebar;
[INotifyPropertyChanged]
public abstract partial class SidebarItemViewModel : ISidebarItem
{
    [ObservableProperty]
    private bool _isSelected;
    public abstract string Glyph { get; }
    public abstract string Title { get; protected set; }
    public virtual string GlyphFontFamily { get; } = "Segoe Fluent Icons";
}
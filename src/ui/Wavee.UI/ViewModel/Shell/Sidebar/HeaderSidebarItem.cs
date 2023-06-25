using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.ViewModel.Shell.Sidebar;

public sealed class HeaderSidebarItem : ISidebarItem
{
    public HeaderSidebarItem(string title)
    {
        Title = title;
    }

    public string Title { get; }
}

public sealed class RegularSidebarItem : ISidebarItem
{
    public RegularSidebarItem()
    {

    }
    public RegularSidebarItem(string title, string iconGlyph, string iconFontFamily, Type viewModelType, object parameter)
    {
        Title = title;
        IconGlyph = iconGlyph;
        IconFontFamily = iconFontFamily;
        ViewModelType = viewModelType;
        Parameter = parameter;
    }

    public string Title { get; }
    public string IconGlyph { get; }
    public string IconFontFamily { get; }
    public Type ViewModelType { get; }
    public object Parameter { get;}
}

public sealed class CountedSidebarItem : ObservableObject, ISidebarItem
{
    private int _value;
    public CountedSidebarItem()
    {

    }
    public CountedSidebarItem(RegularSidebarItem inner)
    {
        Inner = inner;
    }

    public RegularSidebarItem Inner { get; }

    public int Value
    {
        get => _value;
        set => this.SetProperty(ref _value, value);
    }
}

public sealed class PlaylistSidebarItem : ISidebarItem
{
    public PlaylistSidebarItem(RegularSidebarItem sidebarItem)
    {
        SidebarItem = sidebarItem;
    }

    public RegularSidebarItem SidebarItem { get; }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Id;

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
    public object Parameter { get; }
}

public sealed class CountedSidebarItem : ObservableObject, ISidebarItem
{
    private int _value;
    public CountedSidebarItem(AudioItemType identifier)
    {
        Identifier = identifier;
    }
    public CountedSidebarItem(RegularSidebarItem inner, AudioItemType identifier)
    {
        Inner = inner;
        Identifier = identifier;
    }
    public AudioItemType Identifier { get; }
    public RegularSidebarItem Inner { get; }

    public int Value
    {
        get => _value;
        set => this.SetProperty(ref _value, value);
    }
}

public sealed class PlaylistSidebarItem : ObservableObject, ISidebarItem
{
    private string _title;

    public PlaylistSidebarItem(string title, string iconGlyph, string iconFontFamily, Type viewModelType, object parameter)
    {
        _title = title;
        IconGlyph = iconGlyph;
        IconFontFamily = iconFontFamily;
        ViewModelType = viewModelType;
        Parameter = parameter;
    }

    public string Title
    {
        get => _title;
        set => this.SetProperty(ref _title, value);
    }
    public string IconGlyph { get; }
    public string IconFontFamily { get; }
    public Type ViewModelType { get; }
    public object Parameter { get; }
}

public sealed class PlaylistFolderSidebarItem : ObservableObject, ISidebarItem
{
    private string _title;
    private bool _isExpanded;

    public PlaylistFolderSidebarItem(string title, bool isExpanded, PlaylistSidebarItem[] playlists)
    {
        _title = title;
        _isExpanded = isExpanded;
        Playlists = playlists;
    }

    public string Title
    {
        get => _title;
        set => this.SetProperty(ref _title, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.SetProperty(ref _isExpanded, value);
    }
    public PlaylistSidebarItem[] Playlists { get; set; }
}
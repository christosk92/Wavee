using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Wavee.UI.Shell.Sidebar;

namespace Wavee.UI.Shell;

public sealed class ShellViewModel : ObservableObject
{
    private ISidebarItem _selectedItem;
    public ObservableCollection<ISidebarItem> SidebarItems { get; }

    public ISidebarItem SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ShellViewModel()
    {
        SidebarItems = new ObservableCollection<ISidebarItem>(BuildSidebar());
        SelectedItem = SidebarItems[1];
    }

    private static IEnumerable<ISidebarItem> BuildSidebar()
    {
        // For You:
        var forYou = new HeaderSidebarItem("for.you.header", "For You");
        var forYouItems = new ISidebarItem[]
        {
            new GeneralSidebarItem("for.you.item.1", "Home", "\uE80F", Option<string>.None),
            new GeneralSidebarItem("for.you.item.2", "Feed", "\uE794", "MediaPlayerIcons.ttf#Media Player Fluent Icons"),
        };

        // Library:
        var library = new HeaderSidebarItem("library.header", "Library");
        var libraryItems = new ISidebarItem[]
        {
            new CountedSidebarItem("library.songs", "Songs", "\uE00B", Option<string>.None, 0),
            new CountedSidebarItem("library.albums", "Albums", "\uE93C", Option<string>.None, 0),
            new CountedSidebarItem("library.artists", "Artists", "\uEBDA", Option<string>.None, 0),
            new CountedSidebarItem("library.podcasts", "Podcasts", "\uEB44", Option<string>.None, 0),
        };

        var playlistHeader = new HeaderSidebarItem("library.playlists.header", "Playlists");

        return
            new ISidebarItem[] { forYou }
            .Concat(forYouItems)
            .Concat(new ISidebarItem[] { library }.Concat(libraryItems))
            .Concat(new ISidebarItem[] { playlistHeader });
    }
}
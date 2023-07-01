using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Id;
using Wavee.UI.Helpers;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.ViewModel.Library;
using Wavee.UI.ViewModel.Playback;
using Wavee.UI.ViewModel.Shell.Sidebar;

namespace Wavee.UI.ViewModel.Shell;

public sealed class ShellViewModel : ObservableObject
{
    private readonly Action<Action> _invokeOnUiThread;

    public ShellViewModel(UserViewModel user, Action<Action> invokeOnUiThread)
    {
        _invokeOnUiThread = invokeOnUiThread;
        User = user;
        Instance = this;
        Library = new LibraryViewModel(user, Added, Removed);
        Playback = new PlaybackViewModel(user);
        SidebarItems = new BulkConcurrentObservableCollection<ISidebarItem>(ConstructDefaultItems(user));
        RightSidebar = new RightSidebarViewModel(user);
        Task.Run(async () =>
        {
            await Library.Initialize();
        });
    }

    private void Removed(AudioItemType audioItemType, int i)
    {
        _invokeOnUiThread(() =>
        {
            var sidebarItem = SidebarItems.FirstOrDefault(x=> x is CountedSidebarItem c && c.Identifier == audioItemType);
            if (sidebarItem is CountedSidebarItem counted)
            {
                counted.Value -= i;
            }
        });
    }

    private void Added(AudioItemType audioItemType, int i)
    {
        _invokeOnUiThread(() =>
        {
            var sidebarItem = SidebarItems.FirstOrDefault(x => x is CountedSidebarItem c && c.Identifier == audioItemType);
            if (sidebarItem is CountedSidebarItem counted)
            {
                counted.Value += i;
            }
        });
    }

    public LibraryViewModel Library { get; }
    public UserViewModel User { get; set; }
    public PlaybackViewModel Playback { get; }
    public RightSidebarViewModel RightSidebar { get; }

    public BulkConcurrentObservableCollection<ISidebarItem> SidebarItems { get; }
    public static ShellViewModel Instance { get; private set; }

    private static IEnumerable<ISidebarItem> ConstructDefaultItems(UserViewModel user)
    {
        // Header: For You
        // -> Home
        // -> Feed

        // Header: Your Library
        // -> Saved songs
        // -> Albums
        // -> Artists
        // -> Podcasts

        // Header: Playlists
        // -> Dynamic playlists


        //
        const string fluentIcons = "Segoe Fluent Icons";
        const string mediaPlayerIcons = "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons";

        //For You:
        var header = new HeaderSidebarItem(title: "For You");
        var home = new RegularSidebarItem(title: "Home", iconGlyph: "\uE10F", iconFontFamily: fluentIcons, viewModelType: typeof(HomeViewModel), null);
        var feed = new RegularSidebarItem(title: "Feed", iconGlyph: "\uE794", iconFontFamily: mediaPlayerIcons, viewModelType: null, null);

        var libraryHeader = new HeaderSidebarItem(title: "Your Library");
        var savedSongs = new RegularSidebarItem(title: "Saved songs", iconGlyph: "\uEB52", iconFontFamily: fluentIcons, viewModelType: null, null);
        var albums = new RegularSidebarItem(title: "Albums", iconGlyph: "\uE93C", iconFontFamily: fluentIcons, viewModelType: null, null); ;
        var artists = new RegularSidebarItem(title: "Artists", iconGlyph: "\uEBDA", iconFontFamily: fluentIcons, viewModelType: null, null);
        var podcasts = new RegularSidebarItem(title: "Podcasts", iconGlyph: "\uEB44", iconFontFamily: fluentIcons, viewModelType: null, null);

        var playlistsHeader = new HeaderSidebarItem(title: "Playlists");

        return new ISidebarItem[]
        {
            header,
            home,
            feed,
            libraryHeader,
            new CountedSidebarItem(savedSongs, AudioItemType.Track),
            new CountedSidebarItem(albums, AudioItemType.Album),
            new CountedSidebarItem(artists, AudioItemType.Artist),
            new CountedSidebarItem(podcasts, AudioItemType.PodcastShow),
            playlistsHeader,
        };
    }

}
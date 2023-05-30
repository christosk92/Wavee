using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Sidebar.Items;

namespace Wavee.UI.WinUI.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView(User user)
    {
        this.InitializeComponent();
        var songLibraryItem = new CountedSidebarItem
        {
            Icon = "\uE00B",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Songs",
            Count = 0,
            ViewType = null,
        };
        var albumLibraryItem = new CountedSidebarItem
        {
            Icon = "\uE93C",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Albums",
            Count = 0,
            ViewType = null,
        };
        var artistsLibraryItem = new CountedSidebarItem
        {
            Icon = "\uEBDA",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Artists",
            Count = 0,
            ViewType = null,
        };
        var podcastsLibraryItem = new CountedSidebarItem
        {
            Icon = "\uEB44",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Podcasts",
            Count = 0,
            ForceDisable = true,
            ViewType = null
        };
        this.SidebarControl.SidebarItems = new AbsSidebarItemViewModel[]
        {
            new HeaderSidebarItem { Title = "For You" },
            new RegularSidebarItem
            {
                Icon = "\uE10F",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Home",
                ViewType = typeof(HomeView)
            },
            new RegularSidebarItem
            {
                Icon = "\uE794",
                IconFontFamily = "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons",
                Title = "Browse",
                ViewType = null
            },
            new HeaderSidebarItem { Title = "Library" },
            songLibraryItem,
            albumLibraryItem,
            artistsLibraryItem,
            podcastsLibraryItem
        };

        void OnLibraryItemAdded(Seq<AudioId> id)
        {
            //inc count
            var tracksAdded = id.Where(c => c.Type is AudioItemType.Track).Count;
            var albumsAdded = id.Where(c => c.Type is AudioItemType.Album).Count;
            var artistsAdded = id.Where(c => c.Type is AudioItemType.Artist).Count;
            var podcastsAdded = id.Where(c => c.Type is AudioItemType.PodcastEpisode).Count;

            songLibraryItem.Count += tracksAdded;
            albumLibraryItem.Count += albumsAdded;
            artistsLibraryItem.Count += artistsAdded;
            podcastsLibraryItem.Count += podcastsAdded;
        }
        void OnLibraryItemRemoved(Seq<AudioId> id)
        {
            //dec count
            var tracksRemoved = id.Where(c => c.Type is AudioItemType.Track).Count;
            var albumsRemoved = id.Where(c => c.Type is AudioItemType.Album).Count;
            var artistsRemoved = id.Where(c => c.Type is AudioItemType.Artist).Count;
            var podcastsRemoved = id.Where(c => c.Type is AudioItemType.PodcastEpisode).Count;

            songLibraryItem.Count -= tracksRemoved;
            albumLibraryItem.Count -= albumsRemoved;
            artistsLibraryItem.Count -= artistsRemoved;
            podcastsLibraryItem.Count -= podcastsRemoved;
        }


        ViewModel = new ShellViewModel(user, OnLibraryItemAdded, OnLibraryItemRemoved);
    }
    public ShellViewModel ViewModel { get; }
}
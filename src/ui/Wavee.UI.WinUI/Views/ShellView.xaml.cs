using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CommunityToolkit.WinUI.UI.Controls;
using Eum.Spotify;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Sidebar.Items;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView(
        WaveeUIRuntime runtime,
        User userId)
    {
        Instance = this;
        this.InitializeComponent();
        var songLibraryItem = new CountedSidebarItem
        {
            Icon = "\uE00B",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Songs",
            Count = 0,
            Slug = "songs"
        };
        var albumLibraryItem = new CountedSidebarItem
        {
            Icon = "\uE93C",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Albums",
            Count = 0,
            Slug = "albums"
        };
        var artistsLibraryItem = new CountedSidebarItem
        {
            Icon = "\uEBDA",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Artists",
            Count = 0,
            Slug = "artists"
        };
        var podcastsLibraryItem = new CountedSidebarItem
        {
            Icon = "\uEB44",
            IconFontFamily = "Segoe MDL2 Assets",
            Title = "Podcasts",
            Count = 0,
            Slug = "podcasts"
        };
        SidebarControl.SidebarItems = new AbsSidebarItemViewModel[]
        {
            new HeaderSidebarItem { Title = "For You" },
            new RegularSidebarItem
            {
                Icon = "\uE10F",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Home",
                Slug = "home"
            },
            new RegularSidebarItem
            {
                Icon = "\uE794",
                IconFontFamily = "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons",
                Title = "Browse",
                Slug = "browse"
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

        ViewModel = new ShellViewModel<WaveeUIRuntime>(runtime, userId, OnLibraryItemAdded, OnLibraryItemRemoved);
        NavigationService = new NavigationService(SidebarControl.NavigationFrame);
        NavigationService.Navigated += NavigationService_Navigated;
    }

    public void NavigationService_Navigated(object sender, (Type tp, object prm) e)
    {
        SidebarControl.SetSelected(e.tp, e.prm);
    }

    public static NavigationService NavigationService { get; set; }
    public ShellViewModel<WaveeUIRuntime> ViewModel { get; }
    public static ShellView Instance { get; private set; }
}
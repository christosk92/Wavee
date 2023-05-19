using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CommunityToolkit.WinUI.UI.Controls;
using Eum.Spotify;
using Wavee.Core.Contracts;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Sidebar.Items;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView(WaveeUIRuntime runtime, User userId)
    {
        ViewModel = new ShellViewModel<WaveeUIRuntime>(runtime, userId);
        this.InitializeComponent();
        NavigationService = new NavigationService(NavigationFrame);
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
            new CountedSidebarItem
            {
                Icon = "\uE00B",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Songs",
                Count = 0,
                Slug = "songs"
            },
            new CountedSidebarItem
            {
                Icon = "\uE93C",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Albums",
                Count = 0,
                Slug = "albums"
            },
            new CountedSidebarItem
            {
                Icon = "\uEBDA",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Artists",
                Count = 0,
                Slug = "artists"
            },
            new CountedSidebarItem
            {
                Icon = "\uEB44",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Podcasts",
                Count = 0,
                Slug = "podcasts"
            }
        };
    }
    public static NavigationService NavigationService { get; set; }
    public ShellViewModel<WaveeUIRuntime> ViewModel { get; }
}
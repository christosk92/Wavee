using Eum.Spotify;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Sidebar.Items;

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
                Title = "Home"
            },
            new RegularSidebarItem
            {
                Icon = "\uE794",
                IconFontFamily = "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons",
                Title = "Browse"
            },
            new HeaderSidebarItem { Title = "Library" },
            new CountedSidebarItem
            {
                Icon = "\uE00B",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Songs",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uE93C",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Albums",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uEBDA",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Artists",
                Count = 0
            },
            new CountedSidebarItem
            {
                Icon = "\uEB44",
                IconFontFamily = "Segoe MDL2 Assets",
                Title = "Podcasts",
                Count = 0
            }
        };
    }
    public static NavigationService NavigationService { get; set; }
    public ShellViewModel<WaveeUIRuntime> ViewModel { get; }
}
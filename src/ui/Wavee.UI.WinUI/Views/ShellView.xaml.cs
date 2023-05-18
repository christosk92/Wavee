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

        ViewModel.Playback.RegisterPositionCallback(1000, c =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ProgressSlider.Value = c;
                TimeElapsedElement.Text = FormatDuration(TimeSpan.FromMilliseconds(c));
            });
        });
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

    public Uri GetImageFor(ITrack track)
    {
        if (track is not null && track.Album.Artwork.Length > 0)
        {
            return new Uri(track.Album.Artwork[0].Url);
        }
        else
        {
            return new Uri("ms-appx:///Assets/album_placeholder.png");
        }
    }

    public IEnumerable<MetadataItem> TransformItemsForMetadata(ITrack track)
    {
        if (track is null) return Enumerable.Empty<MetadataItem>();

        return track.Artists.Select(c => new MetadataItem
        {
            Label = c.Name
        });
    }

    public string FormatDuration(ITrack track)
    {
        if (track is null) return "--:--";
        return FormatDuration(track.Duration);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var i = (int)duration.TotalMilliseconds;
        return $"{i / 60000:00}:{i / 1000 % 60:00}";
    }
}
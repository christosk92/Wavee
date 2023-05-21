using System;
using System.Linq;
using DynamicData;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualBasic.Devices;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Models;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Flyouts;

public static class FlyoutExtensions
{
    public static MenuFlyout ConstructFlyout(this AudioId id)
    {
        return id.Type switch
        {
            AudioItemType.Album => CreateAlbumFlyout(id),
            AudioItemType.Artist => CreateArtistFlyout(id),
            AudioItemType.Playlist => CreatePlaylistFlyout(id),
            AudioItemType.Track => CreateTrackFlyout(id),
            _ => throw new ArgumentOutOfRangeException(nameof(id.Type), id.Type, null)
        };
    }

    private static MenuFlyout CreateAlbumFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(id));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(id));
        baseFlout.Items.Add(AddToPlaylist(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreateArtistFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(id));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(id));
        baseFlout.Items.Add(AddToPlaylist(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreatePlaylistFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(id));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(id));
        baseFlout.Items.Add(AddToPlaylist(id, "Copy to other playlist"));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreateTrackFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(id));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(id));
        baseFlout.Items.Add(AddToPlaylist(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }

    private static MenuFlyoutSubItem Share(AudioId id)
    {
        var item = new MenuFlyoutSubItem
        {
            Text = "Share",
            Icon = new FontIcon
            { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE72D" }
        };
        return item;
    }

    private static MenuFlyoutSubItem AddToPlaylist(AudioId id, string title = "Add to playlist")
    {
        var playlists = ShellViewModel<WaveeUIRuntime>.Instance
            .PlaylistsVm
            .Playlists;

        var subItem = new MenuFlyoutSubItem
        {
            Text = title,
            Icon = new FontIcon
            {
                FontFamily =
                    new Microsoft.UI.Xaml.Media.FontFamily(
                        "/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"),
                Glyph = "\uE710"
            },
        };

        void constructSubItems(MenuFlyoutSubItem into, PlaylistInfo playlist)
        {
            if (playlist.IsFolder)
            {
                var subItem = new MenuFlyoutSubItem
                {
                    Text = playlist.Name,
                    Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"), Glyph = "\uE8B7" },
                };

                foreach (var child in playlist.SubItems)
                {
                    constructSubItems(subItem, child);
                }

                into.Items.Add(subItem);
            }

            else
            {
                var item = new MenuFlyoutItem
                {
                    Text = playlist.Name
                };

                into.Items.Add(item);
            }
        }
        foreach (var playlist in playlists)
        {
            constructSubItems(subItem, playlist);
        }

        return subItem;
    }

    private static MenuFlyoutItem Library(AudioId id)
    {
        var saved = ShellViewModel<WaveeUIRuntime>.Instance
            .Library.InLibrary(id);

        var item = new MenuFlyoutItem
        {
            Text = saved ? "Remove from library" : "Add to library",
            Icon = saved
                ? new FontIcon
                {
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    Glyph =
                    "\uEB51"
                }
            : new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\uEB52"
            }
        };

        return item;
    }

    private static MenuFlyoutItem AddToQueue(AudioId id) => new MenuFlyoutItem
    {
        Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"), Glyph = "\uE93F" },
        Text = "Add to queue"
    };

    private static MenuFlyoutItem GoToRadio(AudioId id) => new MenuFlyoutItem
    {
        Text = $"Go to radio",
        Icon = new FontIcon
        {
            FontFamily =
                new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"),
            Glyph = "\uE93E"
        }
    };
}
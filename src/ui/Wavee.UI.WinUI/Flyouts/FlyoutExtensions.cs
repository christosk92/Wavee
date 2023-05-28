using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualBasic.Devices;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Models;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Flyouts;

public readonly record struct AddToPlaylistRequest(AudioId PlaylistId, 
    string PlaylistRevision,
    Seq<AudioId> Ids);

public static class FlyoutExtensions
{
    static FlyoutExtensions()
    {
        static async Task<AudioId[]> ContextResolve(AudioId id)
        {
            var contextResolve =
                from mercury in Spotify<WaveeUIRuntime>.Mercury()
                from ctx in mercury.ContextResolve(id.ToString()).ToAff()
                select ctx;

            var result = (await contextResolve.Run(App.Runtime)).ThrowIfFail();

            return result.Pages.SelectMany(x => x.Tracks.Select(f => AudioId.FromUri(f.Uri))).ToArray();
        }

        AddToPlaylistCommand = ReactiveCommand.CreateFromTask<AddToPlaylistRequest>(async (req, ct) =>
        {
            var groups = req.Ids.GroupBy(x => x.Type);

            foreach (var group in groups)
            {
                switch (group.Key)
                {
                    case AudioItemType.Artist:
                        break;
                    case AudioItemType.Playlist:
                    case AudioItemType.Album:
                        {
                            //do a context-resolve
                            var tracks = await ContextResolve(req.Ids[0]);

                            //max of 100 per request
                            //limit to 6 concurrent requests
                            var batches = tracks
                                .Chunk(4000);
                            foreach (var batch in batches)
                            {
                                var result = await Spotify<WaveeUIRuntime>
                                    .AddToPlaylist(req.PlaylistId,
                                        req.PlaylistRevision,
                                        batch.ToSeq(), Option<int>.None)
                                    .Run(App.Runtime);
                                if (result.IsFail)
                                {

                                }
                            }
                            break;
                        }
                    case AudioItemType.Track:
                        {
                            var result = await Spotify<WaveeUIRuntime>.AddToPlaylist(req.PlaylistId,
                                    req.PlaylistRevision,
                                    req.Ids, Option<int>.None)
                                .Run(App.Runtime);
                            break;
                        }
                }
            }
        });
    }

    public static MenuFlyout ConstructFlyout(this Seq<AudioId> ids)
    {
        if (ids.Length == 1)
            return ids.Head().ConstructFlyout();

        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(ids));
        baseFlout.Items.Add(Library(ids));
        baseFlout.Items.Add(AddToPlaylist(ids));

        return baseFlout;
    }
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
        baseFlout.Items.Add(AddToQueue(Seq1(id)));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(Seq1(id)));
        baseFlout.Items.Add(AddToPlaylist(Seq1(id)));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreateArtistFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(Seq1(id)));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(Seq1(id)));
        baseFlout.Items.Add(AddToPlaylist(Seq1(id)));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreatePlaylistFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(Seq1(id)));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(Seq1(id)));
        baseFlout.Items.Add(AddToPlaylist(Seq1(id), "Copy to other playlist"));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Share(id));
        return baseFlout;
    }
    private static MenuFlyout CreateTrackFlyout(AudioId id)
    {
        var baseFlout = new MenuFlyout();
        baseFlout.Items.Add(AddToQueue(Seq1(id)));
        baseFlout.Items.Add(GoToRadio(id));
        baseFlout.Items.Add(new MenuFlyoutSeparator());

        baseFlout.Items.Add(Library(Seq1(id
            )));
        baseFlout.Items.Add(AddToPlaylist(Seq1(id)));
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

    private static MenuFlyoutSubItem AddToPlaylist(Seq<AudioId> ids, string title = "Add to playlist")
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
                if (playlist.OwnerId == ShellViewModel<WaveeUIRuntime>.Instance.User.Id)
                {
                    var item = new MenuFlyoutItem
                    {
                        Text = playlist.Name,
                        Command = AddToPlaylistCommand,
                        CommandParameter = new AddToPlaylistRequest(AudioId.FromUri(playlist.Id),
                            playlist.RevisionId,
                            ids)
                    };

                    into.Items.Add(item);
                }
            }
        }
        foreach (var playlist in playlists)
        {
            constructSubItems(subItem, playlist);
        }

        return subItem;
    }

    public static ICommand AddToPlaylistCommand { get; }

    private static MenuFlyoutItem Library(Seq<AudioId> ids)
    {
        var saved = ids.All(x=> ShellViewModel<WaveeUIRuntime>.Instance
            .Library.InLibrary(x));

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

    private static MenuFlyoutItem AddToQueue(Seq<AudioId> ids) => new MenuFlyoutItem
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
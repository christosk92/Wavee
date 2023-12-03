using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Messaging;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.UI.Features.Library.Notifications;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Library.ViewModels;

public sealed class LibrariesViewModel : NavigationItemViewModel,
    INotificationHandler<LibraryItemAddedNotification>,
    INotificationHandler<LibraryItemRemovedNotification>
{
    public LibrariesViewModel(
        LibrarySongsViewModel songs,
        LibraryArtistsViewModel artists,
        LibraryAlbumsViewModel albums,
        LibraryPodcastsViewModel podcasts)
    {
        Songs = songs;
        Artists = artists;
        Albums = albums;
        Podcasts = podcasts;
        Children = new NavigationItemViewModel[]
        {
            songs,
            artists,
            albums,
            podcasts
        };

        SelectedItem = songs;
    }

    public override NavigationItemViewModel[] Children { get; }
    public NavigationItemViewModel Songs { get; }
    public NavigationItemViewModel Albums { get; }
    public NavigationItemViewModel Artists { get; }
    public NavigationItemViewModel Podcasts { get; }
    public async ValueTask Handle(LibraryItemAddedNotification message, CancellationToken cancellationToken)
    {
        var groupedBy = message.Items.GroupBy(x => x.Type);
        foreach (var group in groupedBy)
        {
            switch (group.Key)
            {
                case SpotifyItemType.Artist:
                    {
                        await (Artists as LibraryArtistsViewModel)!.Add(group.Count());
                        break;
                    }
            }
        }
    }

    public ValueTask Handle(LibraryItemRemovedNotification message, CancellationToken cancellationToken)
    {
        var groupedBy = message.Id.GroupBy(x => x.Type);
        foreach (var group in groupedBy)
        {
            switch (group.Key)
            {
                case SpotifyItemType.Artist:
                    {
                        (Artists as LibraryArtistsViewModel)!.Remove(group.Select(f => f.Id).ToImmutableArray());
                        break;
                    }
            }
        }
        return ValueTask.CompletedTask;
    }
}
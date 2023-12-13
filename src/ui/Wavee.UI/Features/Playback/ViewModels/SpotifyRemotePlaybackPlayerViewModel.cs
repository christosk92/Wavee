using System.Collections.Immutable;
using Mediator;
using Wavee.Domain.Exceptions;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Domain.State;
using Wavee.UI.Features.Library.Notifications;
using Wavee.UI.Features.Navigation;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playback.ViewModels;

internal sealed class SpotifyRemotePlaybackPlayerViewModel : PlaybackPlayerViewModel
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly IUIDispatcher _dispatcher;
    private readonly IMediator _mediator;
    public SpotifyRemotePlaybackPlayerViewModel(
        ISpotifyClient spotifyClient,
        IUIDispatcher dispatcher,
        IMediator mediator,
        INavigationService navigationService,
        RightSidebarLyricsViewModel lyricsRightSidebarViewModel) : base(dispatcher, navigationService, lyricsRightSidebarViewModel)
    {
        _spotifyClient = spotifyClient;
        _dispatcher = dispatcher;
        _mediator = mediator;
        spotifyClient.PlaybackStateChanged += SpotifyClientOnPlaybackStateChanged;
        spotifyClient.Library.ItemAdded += LibraryOnItemAdded;
        spotifyClient.Library.ItemRemoved += LibraryOnItemRemoved;
    }

    private void LibraryOnItemRemoved(object? sender, IReadOnlyCollection<SpotifyId> e)
    {
        _dispatcher.Invoke(() =>
        {
            _mediator.Publish(new LibraryItemRemovedNotification()
            {
                Id = e.Select(x => (x.ToString(), x.Type))
            });
        });
    }

    private void LibraryOnItemAdded(object? sender, IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>> e)
    {
        _dispatcher.Invoke(() =>
        {
            _mediator.Publish(new LibraryItemAddedNotification()
            {
                Items = e.Select(x => (x.Item.ToString(),
                    x.Item.Type,
                    x.AddedAt))
            });
        });
    }

    private async void SpotifyClientOnPlaybackStateChanged(object? sender, SpotifyPlaybackState e)
    {
        try
        {
            if (e.TrackInfo is null)
            {
                //TODO: REset playback
                return;
            }

            var trackId = e.TrackInfo.Value.Uri;
            var trackMetadata = await _spotifyClient.Tracks.GetTrack(SpotifyId.FromUri(trackId));
            const string url = "https://i.scdn.co/image/";
            var albumCovers = trackMetadata.Album.CoverGroup.Image.Select(c =>
            {
                var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
                var hex = id.ToBase16();

                return ($"{url}{hex}", c.Width);
            }).ToArray();
            var smallest = albumCovers.OrderBy(c => c.Width).First();
            _dispatcher.Invoke(() =>
            {
                HasPlayback = e.IsActive;
                Duration = TimeSpan.FromMilliseconds(trackMetadata.Duration);
                CoverSmallImageUrl = smallest.Item1;
                Title = trackMetadata.Name;
                Artists = trackMetadata.Artist.Select(a => (SpotifyId.FromRaw(a.Gid.Span, SpotifyItemType.Artist).ToString(), a.Name)).ToArray();
                Id = trackId;
            });

            if (e.IsPaused)
            {
                base.Pause();
            }
            else
            {
                base.Resume(e.Position);
            }

            _dispatcher.Invoke(() =>
            {
                base.OnPlaybackChanged();
            });

        }
        catch (TrackNotFoundException)
        {

        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _spotifyClient.PlaybackStateChanged -= SpotifyClientOnPlaybackStateChanged;
            _spotifyClient.Library.ItemAdded -= LibraryOnItemAdded;
            _spotifyClient.Library.ItemRemoved -= LibraryOnItemRemoved;
        }
    }
}
using System.Collections.Immutable;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Domain.State;
using Wavee.UI.Features.Library.Notifications;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playback.ViewModels;

internal sealed class SpotifyPlaybackPlayerViewModel : PlaybackPlayerViewModel
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly IUIDispatcher _dispatcher;
    private readonly IMediator _mediator;
    public SpotifyPlaybackPlayerViewModel(
        ISpotifyClient spotifyClient,
        IUIDispatcher dispatcher, IMediator mediator) : base(dispatcher)
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
            Artists = trackMetadata.Artist.Select(a => a.Name).ToArray();
        });

        if (e.IsPaused)
        {
            base.Pause();
        }
        else
        {
            base.Resume(e.Position);
        }
    }
}
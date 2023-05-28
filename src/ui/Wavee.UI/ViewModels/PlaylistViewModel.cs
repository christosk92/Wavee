using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.ViewModels;

public sealed class PlaylistViewModel<R> : ReactiveObject, INavigableViewModel
    where R : struct, HasSpotify<R>
{
    private readonly R runtime;
    private PlaylistViewData _playlist;
    private readonly SourceCache<PlaylistTrackInfo, AudioId> _items = new(s => s.Data.Id);
    private IDisposable _listener;
    private IDisposable _cleanup;
    private ReadOnlyObservableCollection<PlaylistTrackVm> _data;
    private string? _searchText;
    private PlaylistTrackSortType _sortParameters;

    public PlaylistViewModel(R runtime)
    {
        this.runtime = runtime;
    }

    public PlaylistViewData Playlist
    {
        get => _playlist;
        set => this.RaiseAndSetIfChanged(ref _playlist, value);
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId playlistId)
            return;

        //playlists in spotify have a revision
        //check if we have the playlist in cache
        //we assume this is the latest revision
        //in the background, fetch the playlist again and do a diff
        //if the diff is empty, we're good
        //if the diff is not empty, we need to update the playlist which shoud be easy.

        //or we dont have the playlist in cache and we need to fetch it the first time
        //and then we need to keep it in cache

        var playlistMaybe = await Spotify<R>.GetPlaylistMaybeCached(playlistId)
            .Run(runtime);
        if (playlistMaybe.IsFail)
        {
            Debugger.Break();
            return;
        }

        var playlist = playlistMaybe.ThrowIfFail();

        Playlist = new PlaylistViewData
        {
            Name = playlist.Playlist.Attributes.Name,
            LargeImage = playlist.Playlist.Attributes.FormatAttributes
                .FirstOrDefault(c => c.Key is "header_image_url_desktop")?
                .Value,
            SmallImage = null,
            Description = playlist.Playlist.Attributes.Description,
            CanChangeDetails = playlist.Playlist.Capabilities
                .CanEditMetadata,
            OwnerUsername = playlist.Playlist.OwnerUsername,
            TotalTracks = (uint)playlist.Playlist.Contents.Items.Count
        };

        //setup a listener
        //when the playlist changes, we need to update the cache
        var firstDelta = new Diff
        {
            FromRevision = ByteString.Empty,
            ToRevision = ByteString.Empty
        };

        _listener = Spotify<R>
            .ObservePlaylist(playlistId)
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .StartWith(firstDelta)
            .SelectMany(async x =>
            {
                if (ReferenceEquals(x, firstDelta))
                {
                    var tracksToQueue = playlist.Playlist
                        .Contents.Items.Select(c => AudioId.FromUri(c.Uri)).ToSeq();
                    if (tracksToQueue.IsEmpty)
                    {
                        return Enumerable.Empty<PlaylistTrackInfo>();
                    }

                    var tracks =
                        tracksToQueue.Select(static async c => await TrackEnqueueService<R>.GetTrack(c));
                    var results = await Task.WhenAll(tracks);

                    var toReturn = new List<PlaylistTrackInfo>(playlist.Playlist
                        .Contents.Items.Count);
                    int index = 0;
                    foreach (var request in playlist.Playlist
                                 .Contents.Items)
                    {
                        var track = results
                            .FirstOrDefault(c => c.Id == AudioId.FromUri(request.Uri));
                        if (track.Id.Id.IsZero)
                        {
                            toReturn.Add(new PlaylistTrackInfo(track, DateTimeOffset.MinValue, false, index));
                        }
                        else
                        {
                            toReturn.Add(new PlaylistTrackInfo(track, request.Attributes.HasTimestamp
                                ? DateTimeOffset.FromUnixTimeMilliseconds(request.Attributes.Timestamp)
                                : DateTimeOffset.MinValue, request.Attributes.HasTimestamp, index));
                        }

                        index++;
                    }

                    return toReturn;
                    //return results.Select(c=> new PlaylistTrackInfo(c, ));
                }
                else
                {
                    return Enumerable.Empty<PlaylistTrackInfo>();
                }
            })
            //.Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(tracks =>
            {
                //TODO
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    foreach (var track in tracks)
                    {
                        innerList.AddOrUpdate(track);
                    }
                });
            });

        PlaylistFetched.TrySetResult();

        var filterApplier =
          this.WhenValueChanged(t => t.SearchText)
              .Throttle(TimeSpan.FromMilliseconds(250))
              .Select(propargs => BuildFilter(propargs))
              .ObserveOn(RxApp.TaskpoolScheduler);

        var sortChange =
            this.WhenValueChanged(t => t.SortParameters)
                .Select(c => c switch
                {
                    _ => SortExpressionComparer<PlaylistTrackVm>
                        .Ascending(x => x.OriginalIndex)
                })
                .ObserveOn(RxApp.TaskpoolScheduler);

        _cleanup = _items
            .Connect()
            //.Filter(c => c.Data.Id.Type is AudioItemType.Track)
            .Transform(x => new PlaylistTrackVm
            {
                AddedAt = x.AddedAt,
                HasAddedAt = x.HasAddedAt,
                Track = x.Data,
                OriginalIndex = x.Index,
            })
            .Filter(filterApplier)
            .Sort(sortChange)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)     // update observable collection bindings
            .DisposeMany()   //dispose when no longer required
            .Subscribe();
    }
    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public PlaylistTrackSortType SortParameters
    {
        get => _sortParameters;
        set => this.RaiseAndSetIfChanged(ref _sortParameters, value);
    }
    public ReadOnlyObservableCollection<PlaylistTrackVm> Data => _data;

    public bool IsSaved
    {
        get => _isSaved;
        set => this.RaiseAndSetIfChanged(ref _isSaved, value);
    }

    public TaskCompletionSource PlaylistFetched = new TaskCompletionSource();
    private bool _isSaved;

    public void OnNavigatedFrom()
    {

    }
    private static Func<PlaylistTrackVm, bool> BuildFilter(string? searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return _ => true;
        return t => t.Track.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
    public void Clear()
    {
        _listener?.Dispose();
    }
}

public enum PlaylistTrackSortType
{
}

public class PlaylistViewData
{
    public required string Name { get; init; } = null!;
    public required string? LargeImage { get; init; } = null!;
    public required string? SmallImage { get; init; } = null!;
    public required string? Description { get; init; } = null!;
    public required bool CanChangeDetails { get; init; }
    public required string OwnerUsername { get; init; }
    public required uint TotalTracks { get; init; }
}
public readonly record struct PlaylistTrackInfo(
    TrackOrEpisode Data, DateTimeOffset AddedAt, bool HasAddedAt, int Index);

public class PlaylistTrackVm
{
    public int OriginalIndex { get; init; }
    public TrackOrEpisode Track { get; init; }
    public DateTimeOffset AddedAt { get; init; }
    public bool HasAddedAt { get; init; }

    public string GetSmallestImage(TrackOrEpisode trackOrEpisode)
    {
        return Track.Value.Match(
            Left: ep => "",
            Right: tr => tr.Album.Artwork.OrderBy(i => i.Width).First().Url
        );
    }
}
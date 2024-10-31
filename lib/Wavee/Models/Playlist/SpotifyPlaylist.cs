using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using Eum.Spotify.playlist4;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Metadata;
using Wavee.Services.Playback.Remote;
using Wavee.Services.Playlists;

namespace Wavee.Models.Playlist
{
    /// <summary>
    /// Represents a Spotify playlist with tracks and revision management.
    /// </summary>
    public sealed class SpotifyPlaylist : SpotifyPlaylistOrFolder
    {
        private string? _revision;
        private readonly ISpotifyApiClient _spotifyApiClient;
        private readonly ISpotifyPlaylistRepository _spotifyPlaylistRepository;
        private readonly CompositeDisposable _disposables = new();
        private readonly SourceCache<SpotifyPlaylistTrack, Guid> _items = new(x => x.InstanceId);
        private readonly ReadOnlyObservableCollection<SpotifyPlaylistTrack> _itemsReadOnly;
        private readonly AsyncLock _lock = new();
        private readonly ILogger<SpotifyPlaylistClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyPlaylist"/> class.
        /// </summary>
        /// <param name="id">The unique Spotify ID of the playlist.</param>
        /// <param name="name">The name of the playlist.</param>
        /// <param name="index">The index position of the playlist.</param>
        /// <param name="addedAt">The timestamp when the playlist was added.</param>
        /// <param name="revision">The revision identifier of the playlist.</param>
        /// <param name="spotifyWebsocketState">The Spotify websocket state for handling real-time updates.</param>
        /// <param name="tracks">The initial list of tracks in the playlist.</param>
        /// <param name="spotifyApiClient">The Spotify API client for fetching playlist data.</param>
        /// <param name="spotifyPlaylistRepository">The repository for persisting playlist data.</param>
        /// <param name="logger">The logger instance for logging activities.</param>
        internal SpotifyPlaylist(
            SpotifyId id,
            string name,
            int index,
            DateTimeOffset addedAt,
            string revision,
            ISpotifyWebsocketState spotifyWebsocketState,
            List<SpotifyPlaylistTrack> tracks,
            ISpotifyApiClient spotifyApiClient,
            ISpotifyPlaylistRepository spotifyPlaylistRepository,
            ILogger<SpotifyPlaylistClient> logger)
            : base(id, name, index, addedAt)
        {
            Revision = revision;
            _spotifyApiClient = spotifyApiClient;
            _spotifyPlaylistRepository = spotifyPlaylistRepository;
            _logger = logger;

            // Subscribe to playlist change notifications
            _disposables.Add(
                spotifyWebsocketState
                    .AddMessageHandler($"hm://playlist/v2/playlist/{id.ToBase62()}", PlaylistItemsChanged));

            // Set up track collection with sorting and binding
            var newSub = _items.Connect()
                .AutoRefresh(x => x.Index)
                .Sort(SortExpressionComparer<SpotifyPlaylistTrack>.Ascending(x => x.Index))
                .Bind(out _itemsReadOnly)
                .Subscribe();

            _items.AddOrUpdate(tracks);

            _disposables.Add(newSub);
            _disposables.Add(_items);
        }

        /// <summary>
        /// Gets or sets the revision identifier of the playlist.
        /// </summary>
        public string Revision
        {
            get => _revision!;
            set => SetField(ref _revision, value);
        }

        /// <summary>
        /// Gets a read-only collection of tracks in the playlist.
        /// </summary>
        public ReadOnlyObservableCollection<SpotifyPlaylistTrack> Tracks => _itemsReadOnly;

        /// <summary>
        /// Initializes the playlist by fetching its tracks and applying any differences based on revisions.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task Initialize(CancellationToken cancellationToken)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                var tracksInitialized = Tracks.All(x => x.Initialized);
                if (tracksInitialized)
                {
                    // Perform a differential update based on revisions
                    var playlist = await _spotifyApiClient.GetPlaylist(Revision, Id, cancellationToken);
                    if (playlist.Diff is null)
                    {
                        _items.Edit(x =>
                        {
                            foreach (var track in x.Items)
                            {
                                track.Initialized = false;
                            }
                        });
                        _ = Task.Run(async () => await Initialize(cancellationToken));
                        return;
                    }

                    await ApplyDiff(playlist, cancellationToken);
                }
                else
                {
                    // Fetch all tracks since they are not initialized yet
                    var playlist = await _spotifyApiClient.GetPlaylist(null, Id, cancellationToken);
                    _items.Edit(x =>
                    {
                        x.Clear();
                        int index = 0;
                        foreach (var track in playlist.Contents.Items)
                        {
                            var newTrack = CreateSpotifyPlaylistTrack(track, index);
                            x.AddOrUpdate(newTrack);
                            index++;
                        }
                    });

                    // Update the revision
                    Revision = playlist.Revision.ToBase64();
                    var cachedItem = SpotifyPlaylistClient.CreatePlaylistItem(this);
                    await _spotifyPlaylistRepository.SavePlaylist(cachedItem);
                    await _spotifyApiClient.StoreSyncTokenAsync(Id.ToString(), Revision, cancellationToken);
                }
            }
        }

        public async Task InitializeTracksData(CancellationToken cancellationToken)
        {
            await Initialize(cancellationToken);
            // Create a dictionary where we can easily map:
            // ID -> List of Playlist Tracks
            var trackLists = Tracks
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.ToList());

            var toFetchDictionary = trackLists.ToDictionary(x => x.Key, d => d.Key.ItemType switch
            {
                SpotifyItemType.Episode => new SpotifyEpisode(),
                SpotifyItemType.Track => new SpotifyTrack() as SpotifyItem,
            });
            await _spotifyApiClient.GetTracks(toFetchDictionary, cancellationToken);
            // Now populate
            foreach (var fetchedItem in toFetchDictionary)
            {
                var id = fetchedItem.Key;
                var item = fetchedItem.Value;
                if (trackLists.TryGetValue(id, out var tracks))
                {
                    foreach (var track in tracks)
                    {
                        track.Item = item;
                    }
                }
                else
                {
                    switch (item)
                    {
                        case SpotifyTrack track:
                        {
                            foreach (var fetchedId in track.AlternativeIds)
                            {
                                if (trackLists.TryGetValue(fetchedId, out var altTracks))
                                {
                                    foreach (var altTrack in altTracks)
                                    {
                                        altTrack.Item = item;
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SpotifyPlaylist"/>.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Handles playlist item changes received from the Spotify websocket.
        /// </summary>
        /// <param name="message">The websocket message containing the changes.</param>
        /// <param name="parameters">Additional parameters associated with the message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private async Task PlaylistItemsChanged(
            SpotifyWebsocketMessage message,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("PlaylistItemsChanged for {Id}", Id);
            await Initialize(cancellationToken);
        }

        /// <summary>
        /// Applies the differences between the current playlist and a new version from the Spotify API.
        /// </summary>
        /// <param name="playlist">The new playlist content received from the API.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private async Task ApplyDiff(SelectedListContent playlist, CancellationToken cancellationToken)
        {
            if (playlist.Diff is null)
            {
                return;
            }

            foreach (var op in playlist.Diff.Ops)
            {
                switch (op.Kind)
                {
                    case Op.Types.Kind.Add:
                        HandleAddOp(op.Add);
                        break;
                    case Op.Types.Kind.Rem:
                        HandleRemoveOp(op.Rem);
                        break;
                    case Op.Types.Kind.Mov:
                        HandleMoveOp(op.Mov);
                        break;
                    case Op.Types.Kind.UpdateItemAttributes:
                        // Handle attribute updates if necessary
                        break;
                    case Op.Types.Kind.UpdateListAttributes:
                        // Handle list attribute updates if necessary
                        break;
                    default:
                        _logger.LogWarning("Unhandled operation kind: {Kind}", op.Kind);
                        break;
                }
            }

            // Update the revision after applying changes
            Revision = playlist.Diff.ToRevision.ToBase64();
            var cachedItem = SpotifyPlaylistClient.CreatePlaylistItem(this);
            await _spotifyPlaylistRepository.SavePlaylist(cachedItem);
        }

        /// <summary>
        /// Handles 'Add' operations from the playlist diff, inserting new tracks.
        /// </summary>
        /// <param name="opAdd">The 'Add' operation details.</param>
        private void HandleAddOp(Add opAdd)
        {
            try
            {
                int index = opAdd.FromIndex;
                var currentItems = Tracks.OrderBy(x => x.Index).ToList();

                foreach (var item in opAdd.Items)
                {
                    var newTrack = CreateSpotifyPlaylistTrack(item, index);

                    // Insert the new track at the specified index
                    if (index >= currentItems.Count)
                    {
                        currentItems.Add(newTrack);
                    }
                    else
                    {
                        currentItems.Insert(index, newTrack);
                    }

                    index++;
                }

                // Update the indices of all tracks
                UpdateTrackIndices(currentItems);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle add operation");
            }
        }

        /// <summary>
        /// Handles 'Remove' operations from the playlist diff, removing tracks.
        /// </summary>
        /// <param name="opRem">The 'Remove' operation details.</param>
        private void HandleRemoveOp(Rem opRem)
        {
            int fromIndex = opRem.FromIndex;
            int length = opRem.Length;
            var currentItems = Tracks.OrderBy(x => x.Index).ToList();

            // Mark items for removal by setting their index to -1
            for (int i = fromIndex; i < fromIndex + length; i++)
            {
                if (i < currentItems.Count)
                {
                    var item = currentItems[i];
                    item.Index = -1;
                }
            }

            // Remove marked items and update indices
            var updatedItems = currentItems.Where(item => item.Index != -1).ToList();
            UpdateTrackIndices(updatedItems);
        }

        /// <summary>
        /// Handles 'Move' operations from the playlist diff, rearranging tracks.
        /// </summary>
        /// <param name="opMov">The 'Move' operation details.</param>
        private void HandleMoveOp(Mov opMov)
        {
            int fromIndex = opMov.FromIndex;
            int toIndex = opMov.ToIndex;
            int length = opMov.Length;
            var currentItems = Tracks.OrderBy(x => x.Index).ToList();

            // Remove the items to be moved
            var range = currentItems.GetRange(fromIndex, length);
            currentItems.RemoveRange(fromIndex, length);

            // Adjust the destination index if necessary
            if (toIndex > fromIndex)
            {
                toIndex -= length;
            }

            // Insert the items at the new position
            currentItems.InsertRange(toIndex, range);

            // Update the indices of all tracks
            UpdateTrackIndices(currentItems);
        }

        /// <summary>
        /// Updates the indices of the tracks in the playlist.
        /// </summary>
        /// <param name="tracks">The list of tracks to update.</param>
        private void UpdateTrackIndices(List<SpotifyPlaylistTrack> tracks)
        {
            _items.Edit(x =>
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    track.Index = i;
                    track.OriginalIndex = i;
                    x.AddOrUpdate(track);
                }
            });
        }

        /// <summary>
        /// Creates a new <see cref="SpotifyPlaylistTrack"/> from a Spotify API track item.
        /// </summary>
        /// <param name="item">The track item from the Spotify API.</param>
        /// <param name="index">The index of the track in the playlist.</param>
        /// <returns>A fully initialized <see cref="SpotifyPlaylistTrack"/>.</returns>
        private SpotifyPlaylistTrack CreateSpotifyPlaylistTrack(Item item, int index)
        {
            var newTrack = new SpotifyPlaylistTrack
            {
                Index = index,
                OriginalIndex = index,
                Id = SpotifyId.FromUri(item.Uri),
                Initialized = true
            };

            if (item.Attributes.HasItemId && item.Attributes.ItemId.Length > 0)
            {
                newTrack.Uid = SpotifyId.ToBase16(item.Attributes.ItemId);
            }

            if (item.Attributes.HasAddedBy)
            {
                newTrack.AddedBy = item.Attributes.AddedBy;
            }

            if (item.Attributes.HasTimestamp && item.Attributes.Timestamp != 0)
            {
                newTrack.AddedAt = DateTimeOffset.FromUnixTimeMilliseconds(item.Attributes.Timestamp);
            }

            return newTrack;
        }
    }
}
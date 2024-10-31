using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using DynamicData;
using Microsoft.Extensions.Logging;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Library;
using Wavee.Models.Metadata;
using Wavee.Services.Playback.Remote;
using Unit = System.Reactive.Unit;

namespace Wavee.Services
{
    internal sealed class SpotifyLibraryClient : ISpotifyLibraryClient, IDisposable
    {
        private readonly SourceCache<SpotifyLibraryItem, SpotifyId> _libraryItems = new(x => x.Id);
        private readonly ISpotifyApiClient _api;
        private readonly ISpotifyWebsocketState _websocketState;
        private readonly ISpotifyPlaybackClient _playbackClient;
        private readonly ILogger<SpotifyLibraryClient> _logger;

        // Manages all subscriptions to ensure proper disposal
        private readonly CompositeDisposable _disposables = new();

        // Subject to collect incoming SpotifyIds
        private readonly Subject<SpotifyId> _spotifyIdsSubject = new();

        // BehaviorSubject to track initialization status
        private readonly BehaviorSubject<bool> _initializedSubject = new BehaviorSubject<bool>(false);

        private readonly TimeSpan _samplingPeriod = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Indicates whether the client has been initialized.
        /// </summary>
        public IObservable<bool> Initialized => _initializedSubject.AsObservable();

        public SpotifyLibraryClient(SpotifyConfig config, ISpotifyApiClient apiValue,
            ISpotifyWebsocketState websocketStateValue,
            ILogger<SpotifyLibraryClient> createLogger, ISpotifyPlaybackClient playbackClient)
        {
            _api = apiValue;
            _websocketState = websocketStateValue;
            _logger = createLogger;
            _playbackClient = playbackClient;
            _websocketState.Reconnected
                .SelectMany(async _ =>
                {
                    await Initialize();
                    return Unit.Default;
                })
                .Subscribe(
                    _ => { }, // No action needed on successful completion
                    ex => _logger.LogError(ex, "Error initializing SpotifyLibraryClient after reconnection.")
                );

            _websocketState.AddMessageHandler("hm://collection/collection/{user_id}/json", HandleUserCollectionMessage);
            _websocketState.AddMessageHandler("hm://collection/artist/{user_id}/json", HandleUserCollectionMessage);

            // Subscribe to incoming SpotifyIds, batch them, fetch details, and add to cache
            var fetchSubscription = _spotifyIdsSubject
                .Buffer(TimeSpan.FromMilliseconds(200)) // Adjust buffer duration as needed
                .Where(ids => ids.Any())
                .SelectMany(FetchAndAddItemsAsync)
                .Subscribe(
                    _ => { }, // No action needed on successful completion
                    ex => _logger.LogError(ex, "Error fetching SpotifyPlayableItems")
                );

            _disposables.Add(fetchSubscription);
        }

        /// <summary>
        /// Initializes the SpotifyLibraryClient by fetching all existing library items.
        /// This method should be called before processing any add/remove messages.
        /// </summary>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing SpotifyLibraryClient...");
            _libraryItems.Clear();
            await _playbackClient.ConnectToRemoteControl(null, null, cancellationToken: cancellationToken);

            // Step 1: Fetch all existing SpotifyIds from the API
            List<SpotifyId> allIds;
            try
            {
                allIds = await _api.GetAllLibraryIdsAsync(CancellationToken.None);
                _logger.LogInformation("Fetched {Count} SpotifyIds during initialization.", allIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch initial SpotifyIds during initialization.");
                throw; // Rethrow or handle as per your application's needs
            }

            // Step 2: Enqueue all fetched SpotifyIds for processing
            foreach (var id in allIds)
            {
                _spotifyIdsSubject.OnNext(id);
            }


            // Step 3: Mark initialization as complete
            _initializedSubject.OnNext(true);
            _logger.LogInformation("SpotifyLibraryClient initialization completed.");
        }


        /// <inheritdoc />
        public IObservable<IList<SpotifyLibraryNotification>> Notifications =>
            _libraryItems
                .Connect()
                .SelectMany(changeSet => changeSet
                    .Where(change => change.Reason is ChangeReason.Add or ChangeReason.Remove or ChangeReason.Update)
                    .Select(change =>
                        new SpotifyLibraryNotification(
                            change.Current,
                            change.Reason switch
                            {
                                ChangeReason.Add => SpotifyLibraryNotificationType.Added,
                                ChangeReason.Remove => SpotifyLibraryNotificationType.Removed,
                                ChangeReason.Update => SpotifyLibraryNotificationType.Updated,
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        )
                    )
                )
                .Buffer(_samplingPeriod) // Collect notifications within the sampling period
                .Where(batch => batch.Count > 0); // Emit only non-empty batches

        /// <summary>
        /// Exposes a read-only observable cache of SpotifyLibraryItems.
        /// Consumers can subscribe to observe the current state and changes.
        /// </summary>
        public IObservableCache<SpotifyLibraryItem, SpotifyId> LibraryItems => _libraryItems;

        /// <summary>
        /// Handles incoming user collection messages and processes add/remove actions.
        /// This method waits for initialization to complete before processing any messages.
        /// </summary>
        /// <param name="message">The incoming WebSocket message.</param>
        /// <param name="parameters">Parameters extracted from the WebSocket message.</param>
        /// <param name="cancellationtoken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleUserCollectionMessage(SpotifyWebsocketMessage message,
            IDictionary<string, string> parameters,
            CancellationToken cancellationtoken)
        {
            // Wait until initialization is complete
            var r = await _initializedSubject
                .FirstAsync(isInit => isInit);

            var userId = parameters["user_id"];
            _logger.LogDebug("Received user collection message for user {UserId}", userId);
            using var jsonDocument = JsonDocument.Parse(message.Payload);
            var root = jsonDocument.RootElement;
            var items = root.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var addedAtUnix = item.GetProperty("addedAt").GetInt64();
                var removed = item.GetProperty("removed").GetBoolean();
                var identifier = item.GetProperty("identifier").GetString();
                var type = item.GetProperty("type").GetString() switch
                {
                    "album" => SpotifyItemType.Album,
                    "track" => SpotifyItemType.Track,
                    "artist" => SpotifyItemType.Artist,
                };
                var spotifyId = SpotifyId.FromBase62(identifier, type); // always track

                if (removed)
                {
                    _logger.LogDebug("User {UserId} removed track {SpotifyId}", userId, spotifyId);
                    _libraryItems.Remove(spotifyId);
                }
                else
                {
                    _logger.LogDebug("User {UserId} added track {SpotifyId} at {AddedAt}", userId, spotifyId,
                        addedAtUnix);
                    _spotifyIdsSubject.OnNext(spotifyId);
                }
            }
        }

        /// <summary>
        /// Fetches detailed SpotifyPlayableItems for the provided SpotifyIds and adds them to the cache.
        /// </summary>
        /// <param name="idsToFetch">The list of SpotifyIds to fetch.</param>
        /// <returns>An observable that completes when the operation is done.</returns>
        private IObservable<Unit> FetchAndAddItemsAsync(IList<SpotifyId> idsToFetch)
        {
            return Observable.FromAsync(async () =>
            {
                if (idsToFetch == null || !idsToFetch.Any())
                    return;

                // Prepare the dictionary for GetTracks based on SpotifyItemType
                var itemsDictionary = idsToFetch
                    .ToDictionary(
                        id => id,
                        id => id.ItemType switch
                        {
                            SpotifyItemType.Track => new SpotifyTrack() as SpotifyItem,
                            SpotifyItemType.Episode => new SpotifyEpisode(),
                            SpotifyItemType.Album => new SpotifyAlbum(),
                            SpotifyItemType.Artist => new SpotifyArtist(),
                            _ => null // Unsupported item type
                        }
                    )
                    .Where(pair => pair.Value != null)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                try
                {
                    // Fetch detailed SpotifyItems from the API
                    await _api.GetTracks(itemsDictionary, CancellationToken.None);

                    var fetchedItems = new List<SpotifyLibraryItem>();

                    foreach (var id in idsToFetch)
                    {
                        if (itemsDictionary.TryGetValue(id, out var spotifyItem) && spotifyItem != null)
                        {
                            // Map SpotifyItem to SpotifyPlayableItem
                            var playableItem = spotifyItem;

                            if (playableItem != null)
                            {
                                // Create SpotifyLibraryItem with populated Item
                                var libraryItem = new SpotifyLibraryItem(id, DateTimeOffset.UtcNow)
                                {
                                    Item = playableItem
                                };
                                fetchedItems.Add(libraryItem);
                            }
                            else
                            {
                                // Mapping failed; set FetchException
                                var libraryItem = new SpotifyLibraryItem(id, DateTimeOffset.UtcNow)
                                {
                                    FetchException = new Exception("Failed to map SpotifyItem to SpotifyPlayableItem.")
                                };
                                fetchedItems.Add(libraryItem);
                            }
                        }
                        else
                        {
                            // Fetching failed; set FetchException
                            var libraryItem = new SpotifyLibraryItem(id, DateTimeOffset.UtcNow)
                            {
                                FetchException = new Exception("Failed to fetch SpotifyItem.")
                            };
                            fetchedItems.Add(libraryItem);
                        }
                    }

                    // Add or update the fetched items in the SourceCache
                    _libraryItems.AddOrUpdate(fetchedItems);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch Spotify tracks for batch.");

                    // For all ids, set FetchException
                    var failedItems = idsToFetch.Select(id => new SpotifyLibraryItem(id, DateTimeOffset.UtcNow)
                    {
                        FetchException = new Exception("Batch fetching failed.", ex)
                    }).ToList();

                    _libraryItems.AddOrUpdate(failedItems);
                }
            });
        }

        /// <summary>
        /// Disposes of the client and its subscriptions.
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
            _libraryItems.Dispose();
            _spotifyIdsSubject.Dispose();
            _initializedSubject.Dispose();
        }
    }
}
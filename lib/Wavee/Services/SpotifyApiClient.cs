// SpotifyApiClient.cs

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.extendedmetadata;
using Eum.Spotify.playlist4;
using Eum.Spotify.playplay;
using Eum.Spotify.storage;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Spotify.Collection.Proto.V2;
using Spotify.Metadata;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Helpers;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Library;
using Wavee.Models.Metadata;
using Wavee.Models.Playlist;
using Wavee.Repositories;

namespace Wavee.Services;

internal sealed class SpotifyApiClient : ISpotifyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotifyApiClient> _logger;
    private readonly SpotifyConfig _config;
    private readonly ICacheRepository<string> _cacheRepository;
    private readonly ICachingService _cachingService;
    private readonly ICountryProvider _countryProvider;
    private readonly ILibraryRepository _libraryRepository;

    public SpotifyApiClient(SpotifyConfig config,
        HttpClient httpClient,
        ILogger<SpotifyApiClient> logger,
        ICachingService cachingService,
        ICacheRepository<string> cacheRepository,
        ICountryProvider countryProvider,
        ILibraryRepository libraryRepository)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
        _cachingService = cachingService;
        _cacheRepository = cacheRepository;
        _countryProvider = countryProvider;
        _libraryRepository = libraryRepository;
    }


    public async Task<SpotifyTrack> GetTrack(SpotifyId id, bool allowCache = true,
        CancellationToken cancellationToken = default)
    {
        var extensionKind = ExtensionKind.TrackV4;
        var cacheKey = BuildEntityCacheKey(id.ToString(), extensionKind);
        var spotifyTrack = new SpotifyTrack();

        // Check cache
        if (allowCache)
        {
            var cachedBytes = await _cachingService.GetAsync(cacheKey);
            if (cachedBytes != null)
            {
                var trackMessage = Track.Parser.ParseFrom(cachedBytes);
                MapTrack(spotifyTrack, trackMessage, await _countryProvider.GetCountryCode(cancellationToken));

                // Start background validation
                _ = ValidateAndRefreshTrackAsync(id, cacheKey, spotifyTrack, cancellationToken);

                return spotifyTrack;
            }
        }

        // Fetch from API
        await FetchTrackFromApiAsync(id, spotifyTrack, cacheKey, cancellationToken);

        return spotifyTrack;
    }

    public async Task GetTracks(Dictionary<SpotifyId, SpotifyItem> items,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Collect all cache keys
        var cacheKeyToIdMap = new Dictionary<string, SpotifyId>();
        var idToItemMap = new Dictionary<SpotifyId, SpotifyItem>();
        var idToExtensionKindMap = new Dictionary<SpotifyId, ExtensionKind>();
        var country = await _countryProvider.GetCountryCode(cancellationToken);

        foreach (var kvp in items)
        {
            var id = kvp.Key;
            var item = kvp.Value;
            var entityUri = id.ToString();
            var extensionKind = GetExtensionKind(id);
            var cacheKey = BuildEntityCacheKey(entityUri, extensionKind);

            cacheKeyToIdMap[cacheKey] = id;
            idToItemMap[id] = item;
            idToExtensionKindMap[id] = extensionKind;
        }

        // Step 2: Fetch cached items in bulk
        var cachedEntries = await _cachingService.GetAsync(cacheKeyToIdMap.Keys);

        // Prepare dictionaries for items to fetch and items from cache
        var itemsToFetch = new Dictionary<SpotifyId, SpotifyItem>();
        var itemsFromCache = new Dictionary<SpotifyId, SpotifyItem>();

        foreach (var kvp in cacheKeyToIdMap)
        {
            var cacheKey = kvp.Key;
            var id = kvp.Value;

            if (cachedEntries.TryGetValue(cacheKey, out var cachedBytes) && cachedBytes != null)
            {
                var extensionKind = idToExtensionKindMap[id];
                var item = ConstructSpotifyItemFromMessage(cachedBytes, extensionKind, country);
                itemsFromCache[id] = item;
            }
            else
            {
                // Item not in cache; add to fetch list
                itemsToFetch[id] = idToItemMap[id];
            }
        }

        // Step 3: Update items with cached data
        foreach (var kvp in itemsFromCache)
        {
            items[kvp.Key] = kvp.Value;
        }

        // Step 4: Prepare requests for items not in cache
        var entityRequests = new List<EntityRequest>();

        foreach (var id in itemsToFetch.Keys)
        {
            var entityUri = id.ToString();
            var extensionKind = idToExtensionKindMap[id];

            var entityRequest = new EntityRequest
            {
                EntityUri = entityUri
            };
            entityRequest.Query.Add(new ExtensionQuery
            {
                ExtensionKind = extensionKind
            });
            entityRequests.Add(entityRequest);
        }

        // Step 5: Fetch items not in cache from API
        if (entityRequests.Any())
        {
            // batch into 3000 items per request
            var batches = entityRequests
                .Chunk(1000)
                .Select(async batch =>
                {
                    var batchedEntityRequest = new BatchedEntityRequest
                    {
                        Header = new BatchedEntityRequestHeader
                        {
                            Country = country,
                            Catalogue = "premium"
                        },
                        EntityRequest = { batch }
                    };

                    var response = await FetchExtendedMetadataFromApiAsync(batchedEntityRequest, cancellationToken);
                    return response;
                });
            var responses = await Task.WhenAll(batches);
            foreach (var resposne in responses)
            {
                // Step 6: Process response and update items
                await ProcessResponseAndUpdateItems(resposne, itemsToFetch, country);
            }


            // After processing, update the main items dictionary
            foreach (var kvp in itemsToFetch)
            {
                items[kvp.Key] = kvp.Value;
            }
        }
    }

    public async Task<Cluster> PutConnectState(string deviceId, string connectionId, PutStateRequest putState,
        CancellationToken token)
    {
        var url = $"https://spclient.com/connect-state/v1/devices/{deviceId}";
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new ByteArrayContent(putState.ToByteArray())
        };
        // Allow gzip compression and other compression methods
        request.Headers.Add("X-Spotify-Connection-Id", connectionId);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        // Gzip
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await _httpClient.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        await using var responseBytes = await response.Content.ReadAsStreamAsync(token);
        return Cluster.Parser.ParseFrom(responseBytes);
    }

    public async Task<StorageResolveResponse> GetAudioStorageAsync(FileId fileId, bool allowCache,
        CancellationToken token = default)
    {
        var url = $"https://spclient.com/storage-resolve/files/audio/interactive/{fileId.ToBase16()}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        await using var responseBytes = await response.Content.ReadAsStreamAsync(token);
        var resp = StorageResolveResponse.Parser.ParseFrom(responseBytes);
        return resp;
    }

    public async Task<Context> ResolveContext(string contextUrl, CancellationToken cancellationToken)
    {
        //https://gew4-spclient.spotify.com/context-resolve/v1/spotify:artist:1GxkXlMwML1oSg5eLPiAz3?include_video=true
        contextUrl = contextUrl
            .Replace("context://", string.Empty)
            .Replace("hm://", string.Empty);
        var url = $"https://spclient.com/context-resolve/v1/{contextUrl}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // allow gzip compression and other compression methods
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseBytes = await response.Content.ReadAsStringAsync(cancellationToken);
        var ctx = Context.Parser.ParseJson(responseBytes);
        return ctx;
    }

    public async Task<ContextPage> FetchContextPageAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        pageUrl = pageUrl
            .Replace("context://", string.Empty)
            .Replace("hm://", string.Empty);
        var url = $"https://spclient.com/{pageUrl}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // allow gzip compression and other compression methods
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // decode always as utf8
        var reader = new StreamReader(stream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var page = ContextPage.Parser.ParseJson(json);
        //var responseBytes = await response.Content.ReadAsStringAsync(cancellationToken);
        //var page = ContextPage.Parser.ParseJson(responseBytes);

        return page;
    }

    public async Task<List<SpotifyId>> GetAllLibraryIdsAsync(CancellationToken cancellation)
    {
        _logger.LogInformation("Retrieving all SpotifyIds from the repository.");
        await InitializeLibraryAsync(LibraryType.LikedSongs, cancellation);
        await InitializeLibraryAsync(LibraryType.Artists, cancellation);

        var allItems = await _libraryRepository.GetAllItemsAsync(cancellation);
        var allIds = allItems.Select(item => item.Id).ToList();

        _logger.LogInformation("Retrieved a total of {Count} SpotifyIds from the repository.", allIds.Count);
        return allIds;
    }

    /// <summary>
    /// Retrieves the root list of playlists and folders for a user, supporting differential updates.
    /// </summary>
    /// <param name="userId">The Spotify user ID.</param>
    /// <param name="syncToken">Optional synchronization token for fetching diffs.</param>
    /// <param name="decorate">Additional parameters to decorate the response.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="SelectedListContent"/> object containing playlists, folders, and diffs.</returns>
    public async Task<SelectedListContent> GetRootListAsync(string? syncToken = null,
        string decorate = "revision,attributes,length,owner,capabilities,status_code",
        CancellationToken cancellationToken = default)
    {
        var userId = await _countryProvider.UserId(cancellationToken);

        try
        {
            _logger.LogInformation("Fetching root list for User ID: {UserId} with SyncToken: {SyncToken}", userId,
                syncToken ?? "None");

            HttpResponseMessage response;

            if (string.IsNullOrEmpty(syncToken))
            {
                // No syncToken provided; fetch the entire root list
                _logger.LogInformation("Fetching the entire root list for User ID: {UserId}", userId);
                response = await FetchRootList(userId, decorate, cancellationToken);
            }
            else
            {
                // SyncToken provided; fetch the diff
                _logger.LogInformation("Fetching diff for User ID: {UserId} with SyncToken: {SyncToken}", userId,
                    syncToken);
                response = await FetchRootListDiffAsync(userId, syncToken, decorate, cancellationToken);
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var selectedListContent = SelectedListContent.Parser.ParseFrom(stream);

            _logger.LogInformation("Fetched root list (SyncToken Present: {HasDiff})",
                selectedListContent.Diff != null);

            return selectedListContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the root list for User ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<SelectedListContent> GetPlaylist(string? syncToken, SpotifyId id,
        CancellationToken cancellationtoken)
    {
        try
        {
            _logger.LogInformation("Fetching playlist {PlaylistId} with SyncToken: {SyncToken}", id,
                syncToken ?? "None");
            HttpResponseMessage response;

            if (string.IsNullOrEmpty(syncToken))
            {
                _logger.LogInformation("Fetching the entire playlist {PlaylistId}", id);
                response = await FetchPlaylist(id, cancellationtoken);
            }
            else
            {
                _logger.LogInformation("Fetching diff for playlist {PlaylistId} with SyncToken: {SyncToken}", id,
                    syncToken);
                response = await FetchPlaylistDiff(id, syncToken, cancellationtoken);
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationtoken);
            var selectedListContent = SelectedListContent.Parser.ParseFrom(stream);

            _logger.LogInformation("Fetched playlist {PlaylistId} (SyncToken Present: {HasDiff})", id,
                selectedListContent.Diff != null);

            return selectedListContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the playlist {PlaylistId}", id);
            throw;
        }
    }


    /// <summary>
    /// Initializes the Spotify library by fetching all existing library items or fetching differences based on the last sync token.
    /// </summary>
    /// <param name="type">The type of library to initialize (e.g., LikedSongs, Artists).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous initialization operation. The task result contains the library result with all items.</returns>
    /// <exception cref="Exception">Thrown if fetching library items fails.</exception>
    public async Task<LibraryResult> InitializeLibraryAsync(LibraryType type,
        CancellationToken cancellationToken = default)
    {
        if (type == LibraryType.Unknown)
            throw new ArgumentException("LibraryType cannot be Unknown.", nameof(type));

        var syncTokenKey = BuildSyncTokenKey(type.ToString());
        var lastSyncTokenEntry = await _cacheRepository.GetAsync(syncTokenKey);
        var lastSyncToken = lastSyncTokenEntry?.Data != null
            ? Encoding.UTF8.GetString(lastSyncTokenEntry.Data)
            : null;

        LibraryResult libraryResult;

        if (!string.IsNullOrEmpty(lastSyncToken))
        {
            _logger.LogInformation("Fetching library differences for {LibraryType} using sync token.", type);
            libraryResult = await FetchLibraryDifferencesAsync(type, lastSyncToken, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Fetching all library items for {LibraryType} as no sync token exists.", type);
            libraryResult = await FetchAllLibraryItemsAsync(type, cancellationToken);
        }

        // Process deletions
        var deletedItems = libraryResult.Items.Where(x => x.Deleted).ToList();
        if (deletedItems.Any())
        {
            _logger.LogInformation("Removing {Count} deleted items from the repository for {LibraryType}.",
                deletedItems.Count, type);
            await _libraryRepository.DeleteItemsAsync(deletedItems, cancellationToken);
        }

        // Process additions
        var addedItems = libraryResult.Items.Where(x => !x.Deleted).ToList();
        if (addedItems.Any())
        {
            _logger.LogInformation("Adding/Updating {Count} items in the repository for {LibraryType}.",
                addedItems.Count, type);
            await _libraryRepository.AddOrUpdateItemsAsync(addedItems, cancellationToken);
        }

        // Update sync token
        if (!string.IsNullOrEmpty(libraryResult.SyncToken))
        {
            _logger.LogInformation("Updating sync token for {LibraryType}.", type);
            await _cacheRepository.SetAsync(syncTokenKey, new CacheEntry
            {
                Data = Encoding.UTF8.GetBytes(libraryResult.SyncToken),
                Etag = null
            });
        }

        _logger.LogInformation("{LibraryType} initialization completed with {Added} additions and {Removed} removals.",
            type, addedItems.Count, deletedItems.Count);

        // Return the LibraryResult containing all processed items and the new sync token
        return libraryResult;
    }


    /// <summary>
    /// Fetches the differences in the library since the last synchronization.
    /// </summary>
    /// <param name="type">The type of library to fetch differences for.</param>
    /// <param name="syncToken">The last synchronization token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library result with changes.</returns>
    /// <exception cref="Exception">Thrown if the API call fails.</exception>
    private async Task<LibraryResult> FetchLibraryDifferencesAsync(LibraryType type, string syncToken,
        CancellationToken cancellationToken)
    {
        var url = "https://spclient.com/collection/v2/delta";
        var deltaRequest = new DeltaRequest
        {
            Set = type switch
            {
                LibraryType.LikedSongs => "collection",
                LibraryType.Artists => "artist",
                LibraryType.Albums => "album",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            },
            LastSyncToken = syncToken,
            Username = await _countryProvider.UserId(cancellationToken)
        };

        _logger.LogInformation("Sending DeltaRequest to {Url} for {LibraryType} with sync token {SyncToken}.", url,
            type, syncToken);

        var response =
            await _httpClient.PostAsync(url, new ByteArrayContent(deltaRequest.ToByteArray()), cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var deltaResponse = DeltaResponse.Parser.ParseFrom(responseStream);

        var items = new List<SpotifyLibraryItem>();
        foreach (var item in deltaResponse.Items)
        {
            var id = SpotifyId.FromUri(item.Uri);
            var addedAt = DateTimeOffset.FromUnixTimeSeconds(item.AddedAt);
            var libraryItem = new SpotifyLibraryItem(id, addedAt.UtcDateTime)
            {
                Deleted = item.IsRemoved
            };
            items.Add(libraryItem);
        }

        _logger.LogInformation("Fetched {Count} delta items for {LibraryType}.", items.Count, type);

        return new LibraryResult(items.ToArray(), deltaResponse.SyncToken);
    }

    /// <summary>
    /// Fetches all library items for the specified library type, handling pagination.
    /// </summary>
    /// <param name="type">The type of library to fetch items for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library result with all items.</returns>
    /// <exception cref="Exception">Thrown if the API call fails.</exception>
    private async Task<LibraryResult> FetchAllLibraryItemsAsync(LibraryType type, CancellationToken cancellationToken)
    {
        var libraryTypeSet = type switch
        {
            LibraryType.LikedSongs => "collection",
            LibraryType.Artists => "artist",
            LibraryType.Albums => "album",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        string? paginationToken = null;
        string? syncToken = null;
        var allItems = new List<SpotifyLibraryItem>();
        int pageCount = 0;

        do
        {
            var userId = await _countryProvider.UserId(cancellationToken);
            var collectionRequest = new PageRequest
            {
                Username = userId,
                Set = libraryTypeSet,
                Limit = 300 // Adjust based on API's maximum allowed limit
            };

            if (!string.IsNullOrEmpty(paginationToken))
            {
                collectionRequest.PaginationToken = paginationToken;
            }

            _logger.LogInformation(
                "Fetching page {PageNumber} for {LibraryType} with PaginationToken: {PaginationToken}.", pageCount + 1,
                type, paginationToken);

            // Fetching Collection items
            using var response = await DoPageRequest(collectionRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var collectionResponse = PageResponse.Parser.ParseFrom(responseStream);

            paginationToken = collectionResponse.NextPageToken;
            syncToken = collectionResponse.SyncToken;

            foreach (var item in collectionResponse.Items)
            {
                var id = SpotifyId.FromUri(item.Uri);
                var addedAt = DateTimeOffset.FromUnixTimeSeconds(item.AddedAt);
                var libraryItem = new SpotifyLibraryItem(id, addedAt.UtcDateTime);
                allItems.Add(libraryItem);
            }

            _logger.LogInformation("Fetched {Count} items in page {PageNumber} for {LibraryType}.",
                collectionResponse.Items.Count, pageCount + 1, type);
            pageCount++;
        } while (!string.IsNullOrEmpty(paginationToken));

        _logger.LogInformation("Completed fetching all library items for {LibraryType} with {TotalCount} items.", type,
            allItems.Count);

        return new LibraryResult(allItems.ToArray(), syncToken);
    }

    /// <summary>
    /// Fetches the root list of playlists and folders for a user.
    /// </summary>
    /// <param name="userId">The Spotify user ID.</param>
    /// <param name="decorate">Additional parameters to decorate the response.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The HTTP response message.</returns>
    private async Task<HttpResponseMessage> FetchRootList(string userId, string decorate,
        CancellationToken cancellationToken)
    {
        // Construct the URL for fetching the root list
        var url = $"https://spclient.com/playlist/v2/user/{userId}/rootlist?decorate={decorate}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Allow gzip and other compression methods
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        _logger.LogInformation("Sending FetchRootList request to {Url}", url);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> FetchPlaylist(SpotifyId id, CancellationToken cancellationToken)
    {
        var url = $"https://spclient.com/playlist/v2/playlist/{id.ToBase62()}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<HttpResponseMessage> FetchPlaylistDiff(SpotifyId id, string syncToken,
        CancellationToken cancellationToken)
    {
        var revision = new RevisionId(syncToken);
        var url =
            $"https://spclient.com/playlist/v2/playlist/{id.ToBase62()}/diff?revision={revision.ToString()}&handlesContent=";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>
    /// Fetches the differential updates of playlists and folders since the last sync.
    /// </summary>
    /// <param name="userId">The Spotify user ID.</param>
    /// <param name="syncToken">The synchronization token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The HTTP response message.</returns>
    private async Task<HttpResponseMessage> FetchRootListDiffAsync(string userId, string syncToken, string decorate,
        CancellationToken cancellationToken)
    {
        // Construct the URL for fetching diffs
        var revision = new RevisionId(syncToken);

        var url =
            $"https://spclient.com/playlist/v2/user/{userId}/rootlist/diff?revision={revision.ToString()}&decorate={decorate}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Allow gzip and other compression methods
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        _logger.LogInformation("Sending FetchRootListDiff request to {Url} with SyncToken: {SyncToken}", url,
            syncToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> DoPageRequest(PageRequest collectionRequest,
        CancellationToken cancellationToken)
    {
        var url = "https://spclient.com/collection/v2/paging";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(collectionRequest.ToByteArray())
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.collection-v2.spotify.proto"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Spotify", "124500454"));
        request.Headers.Add("App-Platform", "Win32_x86_64");
        request.Headers.Add("Spotify-App-Version", 117300517.ToString());

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private async Task<BatchedExtensionResponse> FetchExtendedMetadataFromApiAsync(
        BatchedEntityRequest request,
        CancellationToken cancellationToken)
    {
        var url = "https://spclient.com/extended-metadata/v0/extended-metadata?market=from_token";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(request.ToByteArray())
        };
        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response =
            await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseBytes = await response.Content.ReadAsStreamAsync(cancellationToken);
        var batchedResponse = BatchedExtensionResponse.Parser.ParseFrom(responseBytes);

        return batchedResponse;
    }

    private async Task ProcessResponseAndUpdateItems(BatchedExtensionResponse response,
        Dictionary<SpotifyId, SpotifyItem> itemsToFetch,
        string userCountry)
    {
        var cacheEntries = new Dictionary<string, (byte[] Value, string? Etag)>();

        foreach (var extensionDataArray in response.ExtendedMetadata)
        {
            var extensionKind = extensionDataArray.ExtensionKind;

            foreach (var extensionData in extensionDataArray.ExtensionData)
            {
                var entityUri = extensionData.EntityUri;
                var spotifyId = ExtractSpotifyIdFromEntityUri(entityUri);

                if (spotifyId != null && itemsToFetch.TryGetValue(spotifyId, out var item))
                {
                    var anyMessage = extensionData.ExtensionData?.Value;

                    if (anyMessage != null)
                    {
                        UpdateSpotifyItemFromMessage(item, anyMessage, userCountry);

                        // Collect the cache entry
                        var cacheKey = BuildEntityCacheKey(entityUri, extensionKind);
                        cacheEntries[cacheKey] = (anyMessage.ToByteArray(), extensionData.Header.Etag);
                    }
                }
            }
        }

        // Perform bulk cache set operation
        await _cachingService.SetAsync(cacheEntries);
    }

    private void UpdateSpotifyItemFromMessage(SpotifyItem item, ByteString message, string userCountry)
    {
        switch (item)
        {
            case SpotifyTrack track:
                MapTrack(track, Track.Parser.ParseFrom(message), userCountry);
                break;
            case SpotifyAlbum album:
                MapAlbum(album, Album.Parser.ParseFrom(message));
                break;
            case SpotifyArtist artist:
                MapArtist(artist, Artist.Parser.ParseFrom(message));
                break;
            // Handle other cases
            default:
                _logger.LogWarning($"Unsupported item type or message type mismatch for item {item.Id}");
                break;
        }
    }

    private SpotifyItem ConstructSpotifyItemFromMessage(byte[] message, ExtensionKind extensionKind, string userCountry)
    {
        switch (extensionKind)
        {
            case ExtensionKind.TrackV4:
                var track = new SpotifyTrack();
                MapTrack(track, Track.Parser.ParseFrom(message), userCountry);
                return track;
            case ExtensionKind.AlbumV4:
                var album = new SpotifyAlbum();
                MapAlbum(album, Album.Parser.ParseFrom(message));
                return album;
            case ExtensionKind.ArtistV4:
                var artist = new SpotifyArtist();
                MapArtist(artist, Artist.Parser.ParseFrom(message));
                return artist;

            // case SpotifyAlbum album when message is Spotify.Metadata.Album albumMessage:
            //     MapAlbum(album, albumMessage);
            //     break;
            // case SpotifyArtist artist when message is Spotify.Metadata.Artist artistMessage:
            //     MapArtist(artist, artistMessage);
            //     break;
            // Handle other cases
            default:
                _logger.LogWarning($"Unsupported item type or message type mismatch for item {message}");
                break;
        }

        return null!;
    }

    private async Task FetchTrackFromApiAsync(SpotifyId id,
        SpotifyTrack fillInto,
        string cacheKey,
        CancellationToken cancellationToken, string? etag = null)
    {
        var userCountry = await _countryProvider.GetCountryCode(cancellationToken);
        var url = $"https://spclient.com/metadata/4/track/{id.ToBase16()}?market=from_token";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(etag))
        {
            // etag might be base64 or base16
            //"MC-uBOT0A=="
            if (!etag.Contains("MC-"))
            {
                // convert to base64 string
                const int SIZE = 4;
                byte[] data = new byte[4];
                for (int i = 0; i < SIZE; i++)
                {
                    data[i] = Convert.ToByte(etag.Substring(i * 2, 2), 16);
                }

                var base64Str = Convert.ToBase64String(data);
                etag = $"\"MC-{base64Str}\"";
            }

            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        }

        var response =
            await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            _logger.LogInformation($"Track ID: {id} is up-to-date.");
            return;
        }

        response.EnsureSuccessStatusCode();

        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var trackMessage = Spotify.Metadata.Track.Parser.ParseFrom(responseBytes);

        // Cache the data using the same cache key
        var newEtag = response.Headers.ETag?.Tag;
        await _cachingService.SetAsync(cacheKey, responseBytes, newEtag);

        MapTrack(fillInto, trackMessage, userCountry);

        // var spotifyTrack = new SpotifyTrack();
        // MapTrack(new SpotifyTrack(), trackMessage);
        //
        // return spotifyTrack;
    }

    private async Task ValidateAndRefreshTrackAsync(SpotifyId id, string cacheKey,
        SpotifyTrack track,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachedEntry = await _cacheRepository.GetAsync(cacheKey);
            var etag = cachedEntry?.Etag;

            await FetchTrackFromApiAsync(id, track, cacheKey, cancellationToken, etag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating cache for track ID: {id}");
        }
    }

    private static void MapAlbum(SpotifyAlbum album, Spotify.Metadata.Album albumMessage)
    {
        album.Id = SpotifyId.FromGid(albumMessage.Gid, SpotifyItemType.Album);
        album.Name = albumMessage.Name;
    }

    private static void MapArtist(SpotifyArtist artist, Spotify.Metadata.Artist artistMessage)
    {
        artist.Id = SpotifyId.FromGid(artistMessage.Gid, SpotifyItemType.Artist);
        artist.Name = artistMessage.Name;
    }

    private static void MapTrack(SpotifyTrack track, Spotify.Metadata.Track trackMessage, string userCountryCode)
    {
        track.Id = SpotifyId.FromGid(trackMessage.Gid, SpotifyItemType.Track);
        track.Name = trackMessage.Name;
        track.Duration = TimeSpan.FromMilliseconds(trackMessage.Duration);
        track.AudioFile = trackMessage.File.ToArray();
        track.DiscNumber = trackMessage.DiscNumber;
        track.TrackNumber = trackMessage.Number;
        track.CanPlay = CanPlayTrack(trackMessage, userCountryCode, out var cannotPlayReason);
        track.CannotPlayReason = cannotPlayReason;
        foreach (var alternative in trackMessage.Alternative)
        {
            track.AlternativeIds.Add(SpotifyId.FromGid(alternative.Gid, SpotifyItemType.Track));
            track.AudioFile = alternative.File.ToArray();
            if (track.AudioFile.Length > 0)
            {
                track.AlternativeIds.Add(track.Id);
                track.Id = SpotifyId.FromGid(alternative.Gid, SpotifyItemType.Track);
                break;
            }
        }

        var album = new SpotifyTrackAlbum();
        if (trackMessage.Album is not null)
        {
            var albumId = SpotifyId.FromGid(trackMessage.Album.Gid, SpotifyItemType.Album);
            album.Id = albumId;
            album.Name = trackMessage.Album.Name;
            album.Type = trackMessage.Album.Type switch
            {
                Album.Types.Type.Album => SpotifyTrackAlbumType.Album,
                Album.Types.Type.Single => SpotifyTrackAlbumType.Single,
                Album.Types.Type.Compilation => SpotifyTrackAlbumType.Compilation,
                Album.Types.Type.Ep => SpotifyTrackAlbumType.EP,
                Album.Types.Type.Audiobook => SpotifyTrackAlbumType.Audiobook,
                Album.Types.Type.Podcast => SpotifyTrackAlbumType.Podcast,
                _ => throw new ArgumentOutOfRangeException()
            };
            album.Images = ImageProcessor.ProcessAlbumCovers(trackMessage);
        }

        SpotifyTrackArtist[] artists = new SpotifyTrackArtist[trackMessage.ArtistWithRole.Count];
        for (int i = 0; i < trackMessage.ArtistWithRole.Count; i++)
        {
            var artist = trackMessage.ArtistWithRole[i];
            artists[i] = new SpotifyTrackArtist
            {
                Id = SpotifyId.FromGid(artist.ArtistGid,
                    SpotifyItemType.Artist),
                Name = artist.ArtistName,
                Role = artist.Role switch
                {
                    ArtistWithRole.Types.ArtistRole.Unknown => SpotifyTrackArtistRole.Unknown,
                    ArtistWithRole.Types.ArtistRole.MainArtist => SpotifyTrackArtistRole.MainArtist,
                    ArtistWithRole.Types.ArtistRole.FeaturedArtist => SpotifyTrackArtistRole.FeaturedArtist,
                    ArtistWithRole.Types.ArtistRole.Remixer => SpotifyTrackArtistRole.Remixer,
                    ArtistWithRole.Types.ArtistRole.Actor => SpotifyTrackArtistRole.Actor,
                    ArtistWithRole.Types.ArtistRole.Composer => SpotifyTrackArtistRole.Composer,
                    ArtistWithRole.Types.ArtistRole.Conductor => SpotifyTrackArtistRole.Conductor,
                    ArtistWithRole.Types.ArtistRole.Orchestra => SpotifyTrackArtistRole.Orchestra,
                    _ => SpotifyTrackArtistRole.Unknown
                }
            };
        }

        track.Album = album;
        track.Artists = artists;
    }

    private static bool CanPlayTrack(Track track, string userCountry, out CannotPlayTrackRestrictionType? reason)
    {
        reason = null;
        foreach (var restriction in track.Restriction)
        {
            if (IsRestricted(userCountry, restriction, out reason))
            {
                return false;
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (now < DateTimeOffset.FromUnixTimeMilliseconds(track.EarliestLiveTimestamp))
        {
            reason = CannotPlayTrackRestrictionType.Embargo;
            return false;
        }

        if (track.Availability.Count is 0)
        {
            reason = null;
            return true;
        }

        foreach (var availability in track.Availability)
        {
            var start = new DateTimeOffset(availability.Start.Year, availability.Start.Month, availability.Start.Day,
                availability.Start.Hour, availability.Start.Minute, 0, TimeSpan.Zero);
            if (now >= start)
            {
                return true;
            }

            reason = CannotPlayTrackRestrictionType.Embargo;
            return false;
        }

        return true;
    }

    private static bool IsRestricted(string userCountry, Restriction restriction,
        out CannotPlayTrackRestrictionType? reason)
    {
        reason = null;
        // A restriction will specify either a whitelast *or* a blacklist,
        // but not both. So restrict availability if there is a whitelist
        // and the country isn't on it.

        if (restriction.HasCountriesAllowed)
        {
            if (restriction.CountriesAllowed.Length > 0)
            {
                if (!IsInList(restriction.CountriesForbidden, userCountry))
                {
                    return true;
                }
            }
        }

        if (restriction.HasCountriesForbidden)
        {
            if (restriction.CountriesForbidden.Length > 0)
            {
                if (IsInList(restriction.CountriesForbidden, userCountry))
                {
                    reason = CannotPlayTrackRestrictionType.CountryBlacklisted;
                    return true;
                }
            }
        }

        reason = null;
        return true;
    }

    /*
     *   private static boolean isInList(@NotNull String list, @NotNull String match) {
            for (int i = 0; i < list.length(); i += 2)
                if (list.substring(i, i + 2).equals(match))
                    return true;

            return false;
        }
     */
    private static bool IsInList(string list, string match)
    {
        for (int i = 0; i < list.Length; i += 2)
        {
            if (list.Substring(i, 2).Equals(match))
            {
                return true;
            }
        }

        return false;
    }

    private ExtensionKind GetExtensionKind(SpotifyId id)
    {
        return id.ItemType switch
        {
            SpotifyItemType.Track => ExtensionKind.TrackV4,
            SpotifyItemType.Album => ExtensionKind.AlbumV4,
            SpotifyItemType.Artist => ExtensionKind.ArtistV4,
            // Add other cases
            _ => throw new NotSupportedException($"Unsupported item type: {id.ItemType}")
        };
    }

    private static SpotifyId ExtractSpotifyIdFromEntityUri(string entityUri)
    {
        return SpotifyId.FromUri(entityUri);
    }

    private string BuildEntityCacheKey(string entityUri, ExtensionKind extensionKind)
    {
        return $"{entityUri}|{extensionKind}";
    }

    private string BuildSyncTokenKey(string type)
    {
        return $"SyncToken_{type}";
    }

    /// <summary>
    /// Stores the synchronization token in the cache repository.
    /// </summary>
    /// <param name="type">The playlist type.</param>
    /// <param name="syncToken">The synchronization token to store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StoreSyncTokenAsync(string type, string syncToken, CancellationToken cancellationToken)
    {
        var syncTokenKey = BuildSyncTokenKey(type);
        var cacheEntry = new CacheEntry
        {
            Data = Encoding.UTF8.GetBytes(syncToken),
            Etag = null // ETag can be managed if necessary
        };

        await _cacheRepository.SetAsync(syncTokenKey, cacheEntry);
        _logger.LogInformation("Stored sync token: {SyncToken} for {PlaylistType}", syncToken, type);
    }

    public Task DeleteSyncTokenAsync(string id, CancellationToken cancellationToken)
    {
        var syncTokenKey = BuildSyncTokenKey(id);
        return _cacheRepository.DeleteAsync(syncTokenKey);
    }

    public async Task<PlayPlayLicenseResponse> GetPlayPlayLicenseAsync(PlayPlayLicenseRequest playPlayLicenseRequest,
        FileId fileId, CancellationToken none)
    {
        var url = $"https://spclient.com/playplay/v1/key/{fileId.ToBase16()}";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(playPlayLicenseRequest.ToByteArray())
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        using var response = await _httpClient.SendAsync(request, none);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(none);
            _logger.LogError("Failed to get PlayPlayLicense: {StatusCode} - {Content}", response.StatusCode, content);
        }

        response.EnsureSuccessStatusCode();
        await using var responseBytes = await response.Content.ReadAsStreamAsync(none);
        return PlayPlayLicenseResponse.Parser.ParseFrom(responseBytes);
    }

    public async Task<TimeSpan> GetServerTimeOffset(CancellationToken cancellationToken)
    {
        try
        {
            var optionsUrl = "https://spclient.com/melody/v1/time";
            // Send OPTIONS request
            var optionsRequest = new HttpRequestMessage(HttpMethod.Options, optionsUrl);
            var optionsResponse = await _httpClient.SendAsync(optionsRequest, cancellationToken);
            if (!optionsResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed notifying server of time request! {code: {StatusCode}, msg: {ReasonPhrase}}",
                    optionsResponse.StatusCode, optionsResponse.ReasonPhrase);
                return TimeSpan.Zero;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed notifying server of time request!");
            return TimeSpan.Zero;
        }

        try
        {
            var getUrl = "https://spclient.com/melody/v1/time";
            // Send GET request
            var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
            var getResponse = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed requesting time! {code: {StatusCode}, msg: {ReasonPhrase}}",
                    getResponse.StatusCode, getResponse.ReasonPhrase);
                return TimeSpan.Zero;
            }

            var responseBody = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(responseBody))
                throw new InvalidOperationException("Response body is null or empty");

            // Parse JSON
            using var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("timestamp", out var timestampElement))
            {
                var serverTimestamp = timestampElement.GetInt64();
                var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var diffMilliseconds = serverTimestamp - currentTimestamp;
                var offset = TimeSpan.FromMilliseconds(diffMilliseconds);

                _logger.LogInformation("Loaded time offset from melody: {Offset}ms", diffMilliseconds);
                return offset;
            }
            else
            {
                _logger.LogError("Failed to find 'timestamp' in server response");
                return TimeSpan.Zero;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed requesting time!");
            return TimeSpan.Zero;
        }
    }

    public async Task<string?> DoCommandAsync(string fromDevice, string toDevice, ISpotifyRemoteCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            //https://gew4-spclient.spotify.com/connect-state/v1/player/command/from/9c67ac2fbe4a39bdff46e2d84b19d9b1b07a2655/to/ce58182f61cb45c0a008c26eda49c8ae
            var url = $"https://spclient.com/connect-state/v1/player/command/from/{fromDevice}/to/{toDevice}";
            var jsonString = command.ToJson();
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var jsonDoc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            var ack_id = jsonDoc.RootElement.GetProperty("ack_id").GetString();
            return ack_id;
        }
        catch (WaveeNetworkException networkException)
        {
            if (networkException.StatusCode is HttpStatusCode.Gone)
            {
                return null;
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command to device {DeviceId}", toDevice);
            return null;
        }
    }


    /// <summary>
    /// Retrieves the synchronization token from the cache repository.
    /// </summary>
    /// <param name="type">The playlist type.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The synchronization token if available; otherwise, null.</returns>
    public async Task<string?> RetrieveSyncTokenAsync(string type, CancellationToken cancellationToken)
    {
        var syncTokenKey = BuildSyncTokenKey(type);
        var cacheEntry = await _cacheRepository.GetAsync(syncTokenKey);
        var syncToken = cacheEntry?.Data != null
            ? ByteString.FromBase64(Encoding.UTF8.GetString(cacheEntry.Data)).ToBase64()
            : null;

        if (!string.IsNullOrEmpty(syncToken))
        {
            _logger.LogInformation("Retrieved sync token: {SyncToken} for {PlaylistType}", syncToken, type);
        }
        else
        {
            _logger.LogInformation("No sync token found for {PlaylistType}. Performing full synchronization.", type);
        }

        return syncToken;
    }


    /// <summary>
    /// Extracts the folder ID from a folder URI.
    /// </summary>
    /// <param name="uri">The folder URI.</param>
    /// <returns>The extracted folder ID.</returns>
    private string ParseFolderIdFromUri(string uri)
    {
        // Example URI: "spotify:start-group:folderId"
        var parts = uri.Split(':');
        if (parts.Length >= 3)
        {
            return parts[2];
        }
        else
        {
            _logger.LogWarning("Unable to parse folder URI: {Uri}", uri);
            return "unknown_folder_id";
        }
    }
}
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.playlist4;
using Eum.Spotify.playplay;
using Eum.Spotify.storage;
using Wavee.Exceptions;
using Wavee.Models.Common;
using Wavee.Models.Metadata;

namespace Wavee.Interfaces;

/// <summary>
/// Defines the contract for interacting with Spotify's Web API, facilitating operations such as retrieving track information, managing playlists, and searching for artists.
/// </summary>
public interface ISpotifyApiClient
{
    /// <summary>
    /// Retrieves detailed information about a specific Spotify track.
    /// </summary>
    /// <param name="id">The unique identifier of the Spotify track to retrieve.</param>
    /// <param name="allowCache">
    /// Indicates whether the method should utilize cached data if available.
    /// If <c>true</c>, the method will return cached data without making an API call.
    /// If <c>false</c>, the method will fetch fresh data from Spotify's API regardless of cache state.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be canceled if necessary.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="SpotifyTrack"/> object with detailed track information.
    /// </returns>
    /// <exception cref="WaveeNetworkException">
    /// Thrown if an error occurs while communicating with Spotify's API.
    /// </exception>
    Task<SpotifyTrack> GetTrack(SpotifyId id, bool allowCache = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information for a collection of Spotify tracks and populates the provided dictionary with the results.
    /// </summary>
    /// <param name="items">
    /// A dictionary where the keys are the unique identifiers of the Spotify tracks to retrieve, and the values are <see cref="SpotifyItem"/> instances that will be populated with detailed track information.
    /// The dictionary should be initialized with the desired track IDs as keys and empty <see cref="SpotifyItem"/> objects as values.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be canceled if necessary.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="WaveeNetworkException">
    /// Thrown if an error occurs while communicating with Spotify's API.
    /// </exception>
    Task GetTracks(Dictionary<SpotifyId, SpotifyItem> items, CancellationToken cancellationToken = default);

    internal Task<Cluster> PutConnectState(string deviceId, string connectionId, PutStateRequest putState,
        CancellationToken token);

    internal Task<StorageResolveResponse> GetAudioStorageAsync(FileId fileId, bool allowCache,
        CancellationToken token = default);

    internal Task<Context> ResolveContext(string contextUrl, CancellationToken cancellationToken);
    internal Task<ContextPage> FetchContextPageAsync(string pageUrl, CancellationToken cancellationToken = default);
    internal Task<List<SpotifyId>> GetAllLibraryIdsAsync(CancellationToken cancellation);
    internal Task<SelectedListContent> GetRootListAsync(string? syncToken = null, string decorate = "revision,attributes,length,owner,capabilities,status_code", CancellationToken cancellationToken = default);
    internal Task<SelectedListContent> GetPlaylist(string? syncToken, SpotifyId id, CancellationToken cancellationtoken);
    internal Task<string?> RetrieveSyncTokenAsync(string id, CancellationToken cancellationToken);

    internal Task StoreSyncTokenAsync(string id, string syncToken, CancellationToken cancellationToken);
    internal Task DeleteSyncTokenAsync(string id, CancellationToken cancellationToken);
    internal Task<PlayPlayLicenseResponse> GetPlayPlayLicenseAsync(PlayPlayLicenseRequest playPlayLicenseRequest, FileId fileId, CancellationToken none);
    internal Task<TimeSpan> GetServerTimeOffset(CancellationToken cancellationToken);

    internal Task<string> DoCommandAsync(string fromDevice, 
        string toDevice, 
        ISpotifyRemoteCommand command,
        CancellationToken cancellationToken);
}
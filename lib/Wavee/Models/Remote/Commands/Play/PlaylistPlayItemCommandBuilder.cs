namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Builder for creating <see cref="PlayItemCommand"/> from a playlist with predefined sorting options.
/// </summary>
public class PlaylistPlayItemCommandBuilder : PlayItemCommandBuilderBase<PlaylistPlayItemCommandBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistPlayItemCommandBuilder"/> class with the specified playlist ID.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    public PlaylistPlayItemCommandBuilder(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
            throw new ArgumentException("Playlist ID cannot be null or empty.", nameof(playlistId));

        // Assuming a standard format for playlist context IDs
        _contextId = $"spotify:playlist:{playlistId}";
    }

    /// <summary>
    /// Sets the playback origin details.
    /// </summary>
    /// <param name="referrerIdentifier">The referrer identifier.</param>
    /// <param name="featureIdentifier">The feature identifier.</param>
    /// <param name="featureVersion">The feature version.</param>
    /// <returns>The current builder instance.</returns>
    public PlaylistPlayItemCommandBuilder WithPlaybackPlayOrigin(string referrerIdentifier, string featureIdentifier,
        string featureVersion)
    {
        var playbackPlayOrigin = new PlaybackPlayOrigin(referrerIdentifier, featureIdentifier, featureVersion);
        return WithPlayOrigin(playbackPlayOrigin);
    }

    /// <summary>
    /// Sets the sorting criteria for the playlist using a predefined SortDescription.
    /// </summary>
    /// <param name="sortDescription">A predefined SortDescription instance.</param>
    /// <returns>The current builder instance.</returns>
    public PlaylistPlayItemCommandBuilder Sort(SortDescription sortDescription)
    {
        if (sortDescription.Equals(default(SortDescription)))
        {
            return this;
        }

        _contextMetadata["list_util_sort"] = sortDescription.ListUtilSort;
        _contextMetadata["sorting.criteria"] = sortDescription.SortingCriteria;
        return this;
    }
}
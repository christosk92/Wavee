namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Builder for creating <see cref="PlayItemCommand"/> from liked songs with predefined sorting options.
/// </summary>
public class LikedSongsPlayItemCommandBuilder : PlayItemCommandBuilderBase<LikedSongsPlayItemCommandBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LikedSongsPlayItemCommandBuilder"/> class.
    /// </summary>
    public LikedSongsPlayItemCommandBuilder(string userId)
    {
        _contextId = $"spotify:user:{userId}:collection";
    }

    /// <summary>
    /// Sets the playback origin details.
    /// </summary>
    /// <param name="referrerIdentifier">The referrer identifier.</param>
    /// <param name="featureIdentifier">The feature identifier.</param>
    /// <param name="featureVersion">The feature version.</param>
    /// <returns>The current builder instance.</returns>
    public LikedSongsPlayItemCommandBuilder WithPlaybackPlayOrigin(string referrerIdentifier, string featureIdentifier,
        string featureVersion)
    {
        var playbackPlayOrigin = new PlaybackPlayOrigin(referrerIdentifier, featureIdentifier, featureVersion);
        return WithPlayOrigin(playbackPlayOrigin);
    }

    /// <summary>
    /// Sets the sorting criteria for liked songs using a predefined SortDescription.
    /// </summary>
    /// <param name="sortDescription">A predefined SortDescription instance.</param>
    /// <returns>The current builder instance.</returns>
    public LikedSongsPlayItemCommandBuilder Sort(SortDescription sortDescription)
    {
        if (sortDescription.Equals(default(SortDescription)))
        {
            sortDescription = SortDescription.DateAdded;
        }

        _contextMetadata["list_util_sort"] = sortDescription.ListUtilSort;
        _contextMetadata["sorting.criteria"] = sortDescription.SortingCriteria;
        return this;
    }
}
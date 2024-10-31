namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Abstract base class for PlayItemCommand builders, providing common functionalities.
/// </summary>
/// <typeparam name="T">The derived builder type.</typeparam>
public abstract class PlayItemCommandBuilderBase<T> : IPlayItemCommandBuilder where T : PlayItemCommandBuilderBase<T>
{
    // Required parameter
    protected string _contextId;

    // Optional parameters with default values
    protected Dictionary<string, string> _contextMetadata = new Dictionary<string, string>();
    protected PlaybackPlayOrigin _playbackPlayOrigin = new PlaybackPlayOrigin();
    protected int? _trackIndex = null;
    protected string? _trackUri = null;
    protected string? _trackUid = null;

    /// <summary>
    /// Sets the context metadata.
    /// </summary>
    /// <param name="contextMetadata">A dictionary containing context metadata.</param>
    /// <returns>The current builder instance.</returns>
    public T WithContextMetadata(Dictionary<string, string> contextMetadata)
    {
        _contextMetadata = contextMetadata ?? throw new ArgumentNullException(nameof(contextMetadata));
        return (T)this;
    }

    /// <summary>
    /// Sets the playback origin.
    /// </summary>
    /// <param name="playbackPlayOrigin">The playback origin details.</param>
    /// <returns>The current builder instance.</returns>
    public T WithPlayOrigin(PlaybackPlayOrigin playbackPlayOrigin)
    {
        _playbackPlayOrigin = playbackPlayOrigin ?? throw new ArgumentNullException(nameof(playbackPlayOrigin));
        return (T)this;
    }

    /// <summary>
    /// Sets the track index to skip to.
    /// </summary>
    /// <param name="trackIndex">The index of the track to skip to.</param>
    /// <returns>The current builder instance.</returns>
    public T WithTrackIndex(int trackIndex)
    {
        if (trackIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(trackIndex), "Track index cannot be negative.");

        _trackIndex = trackIndex;
        return (T)this;
    }

    /// <summary>
    /// Sets the track URI.
    /// </summary>
    /// <param name="trackUri">The URI of the track.</param>
    /// <returns>The current builder instance.</returns>
    public T WithTrackUri(string trackUri)
    {
        _trackUri = trackUri ?? throw new ArgumentNullException(nameof(trackUri));
        return (T)this;
    }

    /// <summary>
    /// Sets the track UID.
    /// </summary>
    /// <param name="trackUid">The UID of the track.</param>
    /// <returns>The current builder instance.</returns>
    public T WithTrackUid(string trackUid)
    {
        _trackUid = trackUid ?? throw new ArgumentNullException(nameof(trackUid));
        return (T)this;
    }

    /// <summary>
    /// Builds and returns an instance of <see cref="PlayItemCommand"/> with the specified parameters.
    /// </summary>
    /// <returns>A new instance of <see cref="PlayItemCommand"/>.</returns>
    PlayItemCommand IPlayItemCommandBuilder.Build()
    {
        return new PlayItemCommand(
            contextId: _contextId,
            contextMetadata: _contextMetadata,
            referrerIdentifier: _playbackPlayOrigin.ReferrerIdentifier,
            featureIdentifier: _playbackPlayOrigin.FeatureIdentifier,
            featureVersion: _playbackPlayOrigin.FeatureVersion,
            trackIndex: _trackIndex,
            trackUri: _trackUri,
            trackUid: _trackUid
        );
    }
}
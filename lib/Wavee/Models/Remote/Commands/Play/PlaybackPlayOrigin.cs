namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Represents the origin of the playback command, encapsulating referrer and feature information.
/// </summary>
public class PlaybackPlayOrigin
{
    /// <summary>
    /// The referrer identifier.
    /// </summary>
    public string ReferrerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The feature identifier.
    /// </summary>
    public string FeatureIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The feature version.
    /// </summary>
    public string FeatureVersion { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackPlayOrigin"/> class.
    /// </summary>
    public PlaybackPlayOrigin() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackPlayOrigin"/> class with specified values.
    /// </summary>
    /// <param name="referrerIdentifier">The referrer identifier.</param>
    /// <param name="featureIdentifier">The feature identifier.</param>
    /// <param name="featureVersion">The feature version.</param>
    public PlaybackPlayOrigin(string referrerIdentifier, string featureIdentifier, string featureVersion)
    {
        ReferrerIdentifier = referrerIdentifier ?? throw new ArgumentNullException(nameof(referrerIdentifier));
        FeatureIdentifier = featureIdentifier ?? throw new ArgumentNullException(nameof(featureIdentifier));
        FeatureVersion = featureVersion ?? throw new ArgumentNullException(nameof(featureVersion));
    }
}
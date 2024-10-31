using Wavee.Enums;
using Wavee.Models.Common;

namespace Wavee.Models.Metadata;

public sealed class SpotifyTrack : SpotifyPlayableItem
{
    /// <summary>
    /// Represents the artists who performed the track.
    /// </summary>
    public SpotifyTrackArtist[] Artists { get; set; }

    /// <summary>
    /// Represents the album containing the track.
    /// </summary>
    public SpotifyTrackAlbum Album { get; set; }

    /// <summary>
    /// Indicates the disc number of the track in a multi-disc album.
    /// </summary>
    public int DiscNumber { get; set; }

    /// <summary>
    /// Indicates the track number on the album.
    /// </summary>
    public int TrackNumber { get; set; }

    /// <summary>
    /// Indicates whether the Spotify track is playable.
    /// </summary>
    public bool CanPlay { get; set; }

    /// <summary>
    /// Indicates the reason why the track cannot be played, represented by a
    /// <see cref="Wavee.Enums.CannotPlayTrackRestrictionType"/> value.
    /// </summary>
    public CannotPlayTrackRestrictionType? CannotPlayReason { get; set; }


    public HashSet<SpotifyId> AlternativeIds { get; set; } = [];
}
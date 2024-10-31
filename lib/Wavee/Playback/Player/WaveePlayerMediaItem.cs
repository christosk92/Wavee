using Wavee.Models.Common;
using Wavee.Models.Metadata;
using Wavee.Playback.Streaming;

namespace Wavee.Playback.Player;

public sealed class WaveePlayerMediaItem : IEquatable<WaveePlayerMediaItem>
{
    public WaveePlayerMediaItem(SpotifyId? id, string? uid, Dictionary<string, string>? metadata = null)
    {
        Id = id;
        Uid = uid;
        Metadata = metadata ?? [];
        if (id is not null)
        {
            Ids = [id.Value];
        }
    }

    public SpotifyId? Id { get; set; }
    public string? Uid { get; set; }
    public TimeSpan? Duration { get; internal set; }
    public Dictionary<string, string> Metadata { get; }
    public int? QueueId { get; set; }
    public bool IsQueued => QueueId.HasValue;
    public HashSet<SpotifyId> Ids { get; set; } = [];

    public bool ContainsId(SpotifyId itemId)
    {
        foreach (var id in Ids ?? [])
        {
            if (id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public void AddIds(params SpotifyId[] ids)
    {
        foreach (var id in ids)
        {
            Ids.Add(id);
        }
    }

    //
    //
    // public string Title { get; private set; }
    // public string ArtistName { get; private set; }
    // public string AlbumTitle { get; private set; }
    // public int AlbumDiscNumber { get; private set; }
    // public int AlbumTrackNumber { get; private set; }
    // public int OriginalIndex { get; private set; }
    // public DateTimeOffset AddedAt { get; private set; }
    // public AudioStream? Stream { get; set; }
    //
    // public void UpdateMetadata(SpotifyTrack track, int originalIndex, DateTimeOffset addedAt)
    // {
    //     //title
    //     //artist_name
    //     //album_title
    //     //album_disc_number
    //     //album_track_number
    //     //original_index
    //     //added_at
    //     Title = track.Name;
    //     ArtistName = track.Artists.FirstOrDefault()?.Name ?? "";
    //     AlbumTitle = track.Album.Name;
    //     AlbumDiscNumber = track.DiscNumber;
    //     AlbumTrackNumber = track.TrackNumber;
    //     OriginalIndex = originalIndex;
    //     AddedAt = addedAt;
    //     Duration = track.Duration;
    // }

    public bool Is(WaveePlayerMediaItem currentMediaItem)
    {
        if (!string.IsNullOrEmpty(Uid) && !string.IsNullOrEmpty(currentMediaItem.Uid))
        {
            return Uid == currentMediaItem.Uid;
        }

        foreach (var id in Ids)
        {
            if (currentMediaItem.ContainsId(id))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Determines whether the specified WaveePlayerMediaItem is equal to the current WaveePlayerMediaItem.
    /// </summary>
    /// <param name="other">The WaveePlayerMediaItem to compare with the current WaveePlayerMediaItem.</param>
    /// <returns>true if the specified WaveePlayerMediaItem is equal to the current WaveePlayerMediaItem; otherwise, false.</returns>
    public bool Equals(WaveePlayerMediaItem? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        // If both Uids are non-null and non-empty, compare them
        if (!string.IsNullOrEmpty(this.Uid) && !string.IsNullOrEmpty(other.Uid))
        {
            return this.Uid == other.Uid;
        }

        // If either Uid is null or empty, compare Ids
        if (this.Ids != null && other.Ids != null)
        {
            // Check if there is any common Id
            foreach (var id in this.Ids)
            {
                if (other.Ids.Contains(id))
                {
                    return true;
                }
            }
        }

        // If no Uid match and no common Ids, they are not equal
        return false;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current WaveePlayerMediaItem.
    /// </summary>
    /// <param name="obj">The object to compare with the current WaveePlayerMediaItem.</param>
    /// <returns>true if the specified object is equal to the current WaveePlayerMediaItem; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as WaveePlayerMediaItem);
    }

    /// <summary>
    /// Returns a hash code for the WaveePlayerMediaItem.
    /// </summary>
    /// <returns>A hash code for the current WaveePlayerMediaItem.</returns>
    public override int GetHashCode()
    {
        // If Uid is available, use its hash code
        if (!string.IsNullOrEmpty(Uid))
        {
            return Uid.GetHashCode();
        }

        // If Uid is not available, combine hash codes of all Ids
        int hash = 17;
        foreach (var id in Ids)
        {
            hash = hash * 31 + id.GetHashCode();
        }

        return hash;
    }

    /// <summary>
    /// Determines whether two specified WaveePlayerMediaItem objects have the same value.
    /// </summary>
    /// <param name="left">The first WaveePlayerMediaItem to compare.</param>
    /// <param name="right">The second WaveePlayerMediaItem to compare.</param>
    /// <returns>true if the value of left is the same as the value of right; otherwise, false.</returns>
    public static bool operator ==(WaveePlayerMediaItem? left, WaveePlayerMediaItem? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified WaveePlayerMediaItem objects have different values.
    /// </summary>
    /// <param name="left">The first WaveePlayerMediaItem to compare.</param>
    /// <param name="right">The second WaveePlayerMediaItem to compare.</param>
    /// <returns>true if the value of left is different from the value of right; otherwise, false.</returns>
    public static bool operator !=(WaveePlayerMediaItem? left, WaveePlayerMediaItem? right)
    {
        return !(left == right);
    }
}
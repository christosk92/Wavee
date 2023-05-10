namespace Wavee.Spotify.Remote;

public readonly record struct SpotifyCollectionUpdateNotificationItem(string Type, bool Unheard, long AddedAt,
    bool Removed, string Identifier);
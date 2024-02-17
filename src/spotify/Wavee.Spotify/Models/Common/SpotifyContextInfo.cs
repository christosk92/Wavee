namespace Wavee.Spotify.Models.Common;

public readonly record struct SpotifyContextInfo(
    string Uri,
    string Url,
    IReadOnlyDictionary<string, string> Metadata,
    SpotifyContextTrackInfo? Track);

public readonly record struct SpotifyContextTrackInfo(
    uint? TrackIndex,
    uint? PageIndex,
    SpotifyId? TrackId,
    string? TrackUid);
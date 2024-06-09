using System.Collections.Generic;

namespace Wavee.UI.Spotify.Common;

public readonly record struct SpotifyContextInfo(
    string Uri,
    string Url,
    IReadOnlyDictionary<string, string> Metadata,
    SpotifyContextTrackInfo? Track);

public readonly record struct SpotifyContextTrackInfo(
    uint? TrackIndex,
    uint? PageIndex,
    ISpotifyId? TrackId,
    string? TrackUid);
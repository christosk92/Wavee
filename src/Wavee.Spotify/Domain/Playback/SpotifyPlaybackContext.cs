using System.Collections.Immutable;

namespace Wavee.Spotify.Domain.Playback;

public readonly struct SpotifyPlaybackContext(string Uri, ImmutableArray<SpotifyPlaybackRestrictionType> Restrictions);
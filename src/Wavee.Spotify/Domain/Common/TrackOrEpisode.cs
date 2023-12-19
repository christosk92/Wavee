using Spotify.Metadata;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Common;

public readonly record struct SpotifyTrackOrEpisode(Track? Track, Episode? Episode, SpotifyId Id);
using Spotify.Metadata;

namespace Wavee.Spotify.Cache.Domain.Tracks;

public record NewTrack(string Id, Track Track, DateTimeOffset DateAdded);
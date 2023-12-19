using Spotify.Metadata;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Podcasts;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Podcast;
using Wavee.UI.Domain.Track;

namespace Wavee.UI.Domain.Playlist;

public readonly record struct PlaylistTrackInfo(string Id)
{
    public required string? AddedBy { get; init; }
    public required DateTimeOffset? AddedAt { get; init; }
    public required string? UniqueItemId { get; init; }

    public required WaveeTrackOrEpisodeOrArtist? Item { get; init; }
    public required DateTimeOffset? LastPlayedAt { get; init; }
}

public readonly record struct WaveeTrackOrEpisodeOrArtist(
    SimpleTrackEntity? Track, 
    SimplePodcastEpisode? Episode, 
    SimpleArtistEntity? Artist,
    string Id);
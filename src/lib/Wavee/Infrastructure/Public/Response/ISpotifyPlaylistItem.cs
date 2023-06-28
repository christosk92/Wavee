using LanguageExt;
using Wavee.Id;

namespace Wavee.Infrastructure.Public.Response;

public interface ISpotifyPlaylistItem
{
    Option<DateTimeOffset> AddedAt { get; }
    SpotifyId Id { get; }
}

public sealed class SpotifyTrackPlaylistItem : ISpotifyPlaylistItem
{
    public required SpotifyPublicTrack Track { get; init; }

    public required Option<DateTimeOffset> AddedAt { get; init; }
    public required SpotifyId Id { get; init; }
}

public sealed class SpotifyEpisodePlaylistItem : ISpotifyPlaylistItem
{
    public required SpotifyPublicEpisode Episode { get; init; }

    public required Option<DateTimeOffset> AddedAt { get; init; }
    public required SpotifyId Id { get; init; }
}
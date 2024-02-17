using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Interfaces;

namespace Wavee.Spotify.Models.Response;

public sealed class SpotifyTrackInfo : ISpotifyPlayableItem
{
    public string Id => Uri.ToString();
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required int Number { get; init; }
    public required int DiscNumber { get; init; }
    public required bool HasLyrics { get; init; }
    public required IReadOnlyList<SpotifyTrackArtist> Artists { get; init; }
    public required SpotifyTrackAlbum Album { get; init; }
    public required TimeSpan Duration { get; init; }
}

public sealed class SpotifyTrackArtist : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
}

public sealed class SpotifyTrackAlbum : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<SpotifyTrackArtist> Artists { get; init; }
    public required string Label { get; init; }
    public required DateOnly ReleaseDate { get; init; }

    public required string? LargeImageUrl { get; init; }
    public required string? MediumImageUrl { get; init; }
    public required string? SmallImageUrl { get; init; }
}
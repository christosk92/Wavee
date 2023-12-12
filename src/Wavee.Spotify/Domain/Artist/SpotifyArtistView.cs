using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Playlists;

namespace Wavee.Spotify.Domain.Artist;

public sealed class SpotifyArtistView
{
    public required SpotifyArtistViewDiscography Discography { get; init; } = null!;
    public required SpotifyArtistViewProfile Profile { get; init; } = null!;
    public required SpotifyArtistViewRelatedContent Related { get; init; } = null!;
    public required SpotifyArtistViewStats Stats { get; init; } = null!;
    public required SpotifyArtistViewVisuals Visuals { get; init; } = null!;
    public required bool Saved { get; init; }
    public required SpotifyId Id { get; init; }
}

public sealed class SpotifyArtistViewVisuals
{
    public required SpotifyImage? HeaderImage { get; init; }
    public required IReadOnlyCollection<SpotifyImage> AvatarImage { get; init; }
}

public sealed class SpotifyArtistViewStats
{
    public required ulong Followers { get; init; }
    public required ulong MonthlyListeners { get; init; }
    public required ushort? Worldrank { get; init; }
}

public sealed class SpotifyArtistViewRelatedContent
{
    public required SpotifyArtistDiscographyGroup<SpotifySimpleAlbum?> AppearsOn { get; init; }
    public required SpotifyArtistDiscographyGroup<SpotifySimplePlaylist?> DiscoveredOn { get; init; }
    public required SpotifyArtistDiscographyGroup<SpotifySimplePlaylist?> FeaturedIn { get; init; }
    public required SpotifyArtistDiscographyGroup<SpotifySimpleArtist?> RelatedArtists { get; init; }
}

public sealed class SpotifyArtistViewProfile
{
    public required string Name { get; init; } = null!;
    public required SpotifyArtistViewPinnedItem? PinnedItem { get; init; }
    public required IReadOnlyCollection<SpotifySimplePlaylist?> Playlists { get; init; }
    public required IReadOnlyDictionary<string, string> SocialLinks { get; init; }
    public required bool Verified { get; init; }
}

public sealed class SpotifyArtistViewPinnedItem
{
    public required string Comment { get; init; } = null!;
    public required ISpotifyItem Item { get; init; } = null!;
    public required string? BackgroundImageUrl { get; init; }
}

public sealed class SpotifyArtistViewDiscography
{
    public required SpotifyArtistDiscographyGroup<SpotifySimpleAlbum?> Albums { get; init; } = null!;
    public required SpotifyArtistDiscographyGroup<SpotifySimpleAlbum?> Compilations { get; init; } = null!;
    public required SpotifyArtistDiscographyGroup<SpotifySimpleAlbum?> Singles { get; init; } = null!;
    public required SpotifySimpleAlbum? LatestRelease { get; init; } = null!;
    public required SpotifyArtistDiscographyGroup<SpotifySimpleAlbum?> PopularReleases { get; init; } = null!;
    public required IReadOnlyCollection<SpotifyArtistTopTrack> TopTracks { get; init; }
}

public sealed class SpotifyArtistTopTrack
{
    public required SpotifyId Id { get; init; }
    public required string Uid { get; init; } = null!;
    public required ulong? Playcount { get; init; }
    public required string Name { get; init; } = null!;
    public required IReadOnlyCollection<SpotifyImage> Images { get; init; }
    public required TimeSpan Duration { get; init; }
    public required SpotifySimpleArtist[] Artists { get; init; }
}

public sealed class SpotifyArtistDiscographyGroup<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public required uint Total { get; init; }
}
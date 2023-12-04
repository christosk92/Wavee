using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Tracks;
using Wavee.UI.Domain;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;

namespace Wavee.UI.Features.Artist.Queries;

public sealed class GetArtistViewQuery : IQuery<ArtistViewResult>
{
    public required string Id { get; init; } = null!;
}

public sealed class ArtistViewResult
{
    public required string Id { get; init; } = null!;
    public required string Name { get; init; } = null!;
    public required string? HeaderImageUrl { get; init; }
    public required string ProfilePictureImageUrl { get; init; }
    public required ulong Followers { get; init; }
    public required ulong MonthlyListeners { get; init; }
    public required IReadOnlyCollection<ArtistTopTrackEntity> TopTracks { get; init; } = null!;
    public required IReadOnlyCollection<ArtistViewDiscographyGroup> Discography { get; init; } = null!;
    public required IReadOnlyCollection<ArtistViewRelatedGroup> RelatedContent { get; init; } = null!;
    public required SimpleAlbumEntity? LatestRelease { get; init; }
}

public sealed class ArtistViewRelatedGroup
{
    public required RelatedGroupType Type { get; init; }
    public required uint Total { get; init; }
    public required IReadOnlyCollection<ArtistViewRelatedItem> Items { get; init; } = null!;
}

public sealed class ArtistViewRelatedItem
{
    public required bool HasValue { get; init; }
    public required IArtistRelatedItem? Item { get; init; }
}

public enum RelatedGroupType
{
    Artist,
    AppearsOnAlbum,
    DiscoveredInPlaylist,
    FeaturedInPlaylist,
}

public sealed class ArtistViewDiscographyGroup
{
    public required DiscographyGroupType Type { get; init; }
    public required uint Total { get; init; }
    public required IReadOnlyCollection<ArtistViewDiscographyItem> Items { get; init; } = null!;
}

public enum DiscographyGroupType
{
    Album,
    Single,
    Compilation,
    PopularRelease
}

public sealed class ArtistViewDiscographyItem
{
    public required bool HasValue { get; init; }
    public required SimpleAlbumEntity? Album { get; init; }
}

public sealed class ArtistTopTrackEntity
{
    public string SmallImage { get; init; }
    public required string Id{ get; init; }
    public required string Name { get; init; }
    public required ulong? Playcount { get; init; }
    public required IReadOnlyCollection<SimpleArtistEntity> Artists { get; init; } = null!;
    public required IReadOnlyCollection<SpotifyImage> Images { get; init; } = null!;
    public required TimeSpan Duration { get; init; }
}
using LiteDB;
using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Domain.Artist;

public sealed class SimpleArtistEntity : IArtistRelatedItem
{
    [BsonId]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string BiggestImageUrl { get; init; }
    public required string SmallestImageUrl { get; init; }
}


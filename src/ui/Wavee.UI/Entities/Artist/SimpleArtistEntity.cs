using LiteDB;

namespace Wavee.UI.Entities.Artist;

public sealed class SimpleArtistEntity
{
    [BsonId]
    public required string Id { get; set; }
    public required string Name { get; set; }
}
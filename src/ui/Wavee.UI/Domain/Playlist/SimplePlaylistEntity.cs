using LiteDB;
using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Domain.Playlist;

public sealed class SimplePlaylistEntity : IArtistRelatedItem
{
    [BsonId]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyCollection<SpotifyImage> Images { get; set; }
}
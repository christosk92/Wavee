using LiteDB;
using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Domain.Album;

public sealed class SimpleAlbumEntity : IArtistRelatedItem
{
    [BsonId]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyCollection<SpotifyImage> Images { get; set; }
    public int? TracksCount { get; set; }
    public required ushort? Year { get; set; }
    public required string? Type { get; set; }
    public IReadOnlyCollection<AlbumTrackEntity>? Tracks { get; set; }
}
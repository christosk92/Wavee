using System.ComponentModel.DataAnnotations;

namespace Wavee.Sqlite.Entities;

public class CachedEpisode
{

}
public class CachedTrack
{
    [Key]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string MainArtistName { get; set; }
    public required string AlbumName { get; set; }
    public required int AlbumDiscNumber { get; set; }
    public required int AlbumTrackNumber { get; set; }
    public required int Duration { get; set; }
    public required string TagsCommaSeparated { get; set; }
    public required string SmallImageId { get; set; }
    public required string MediumImageId { get; set; }
    public required string LargeImageId { get; set; }
    public required byte[] OriginalData { get; set; }
    public required DateTimeOffset CacheExpiration { get; set; }
}
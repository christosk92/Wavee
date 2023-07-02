using System.ComponentModel.DataAnnotations;

namespace Wavee.Sqlite.Entities;

public class CachedPlaylist
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }

    public byte[] Data { get; set; }
    public ICollection<CachedPlaylistTrack> PlaylistTracks { get; set; }
    public string ImageId { get; set; } = string.Empty;
}
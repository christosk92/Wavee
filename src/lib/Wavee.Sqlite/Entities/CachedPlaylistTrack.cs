using System.ComponentModel.DataAnnotations;

namespace Wavee.Sqlite.Entities;

public class CachedPlaylistTrack
{
    [Key]
    public required string PlaylistIdTrackIdCompositeKey { get; set; } = string.Empty;
    public string Uid { get; set; }
    public string Id { get; set; }
    public CachedTrack? Track { get; set; }
    public string MetadataJson { get; set; }
}
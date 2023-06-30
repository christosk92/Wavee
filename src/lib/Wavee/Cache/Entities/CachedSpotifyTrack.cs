using SQLite;

namespace Wavee.Cache.Entities;

public sealed class CachedSpotifyTrack
{
    [PrimaryKey] public string Id { get; set; }
    public byte[] Data { get; set; }
    public DateTimeOffset Expiration { get; set; }
}
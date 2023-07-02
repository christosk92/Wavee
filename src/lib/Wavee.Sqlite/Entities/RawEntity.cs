using System.ComponentModel.DataAnnotations;

namespace Wavee.Sqlite.Entities;

public class RawEntity
{
    [Key]
    public string Id { get; set; }
    public int Type { get; set; }
    public byte[] Data { get; set; }
    public DateTimeOffset Expiration { get; set; }
}
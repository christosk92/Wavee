using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Eum.UI.Models.Entities;

public class TrackCacheEntity
{
    [Key]
    [BsonId]
    public Guid Id { get; set; }
    public string Title { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Image { get; set; }
    public TrackDetailRef[] Artists { get; set; }
    public TrackDetailRef Album { get; set; }
    public string FilePath { get; set; }
}
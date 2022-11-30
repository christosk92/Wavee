using LiteDB;
using System.ComponentModel.DataAnnotations;

namespace Eum.UI.Models.Entities;

public class TrackDetailRef
{
    [Key]
    [BsonId]
    public Guid Id { get; set; }
    public string Name { get; set; }
}
namespace Wavee.UI.Domain.Track;

public sealed class SimpleTrackEntity 
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string? SmallestImageUrl { get; init; }
    public required IReadOnlyCollection<(string Id, string Name)> Artists { get; init; }
    public required (string Id, string Name) Album { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string BiggestImageUrl { get; init; }
}
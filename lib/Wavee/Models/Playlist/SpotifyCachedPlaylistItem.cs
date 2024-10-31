namespace Wavee.Models.Playlist;

public sealed class SpotifyCachedPlaylistItem
{
    public required string Uri { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int Index { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
    public required string RevisionId { get; init; }
    public required List<SpotifyCachedPlaylistTrack> Tracks { get; init; }
}

public sealed class SpotifyCachedPlaylistTrack
{
    public required int Index { get; init; }
    public required string Uri { get; init; }
    public required Dictionary<string, string> Metadata { get; init; }
    public required bool Initialized { get; set; }
}
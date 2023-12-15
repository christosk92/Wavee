namespace Wavee.UI.Domain.Playlist;

public readonly record struct PlaylistTrackInfo(string Id)
{
    public required string? AddedBy { get; init; }
    public required DateTimeOffset? AddedAt { get; init; }
    public required string? UniqueItemId { get; init; }
}

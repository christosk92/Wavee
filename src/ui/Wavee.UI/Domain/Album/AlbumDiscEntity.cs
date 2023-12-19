namespace Wavee.UI.Domain.Album;

public sealed class AlbumDiscEntity
{
    public required ushort Number { get; init; }
    public required IReadOnlyCollection<AlbumTrackEntity> Tracks { get; init; }
}
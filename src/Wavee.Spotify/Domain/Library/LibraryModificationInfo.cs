using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Library;

public sealed class LibraryModificationInfo
{
    public required SpotifyLibaryType Type { get; init; }
    public required bool IsAdded { get; init; }

    public required IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>? Added { get; init; } = null!;

    public required IReadOnlyCollection<SpotifyId>? Removed { get; init; } = null!;
}
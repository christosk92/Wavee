using System.Numerics;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Library;

public sealed class SpotifyLibraryItem<T>
{
    public required T Item { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
    public required DateTimeOffset? LastPlayedAt { get; init; }
}

public sealed class SpotifySongsLibrary
{
    public required IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>> Items { get; init; }
}
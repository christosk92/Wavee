using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Library;

public sealed class SpotifyLibraryItem<T>
{
    public required T Item { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
}
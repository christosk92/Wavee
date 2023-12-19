using System.Numerics;
using Spotify.Metadata;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Tracks;

namespace Wavee.Spotify.Domain.Library;

public sealed class SpotifyLibraryItem<T>
{
    public required T Item { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
    public required DateTimeOffset? LastPlayedAt { get; init; }
}

public sealed class SpotifySongsLibrary
{
    public required IReadOnlyCollection<SpotifyLibraryItem<Track>> Items { get; init; }
}

public sealed class SpotifyArtistsLibrary
{
    public required IReadOnlyCollection<SpotifyLibraryItem<global::Spotify.Metadata.Artist>> Items { get; init; }
}
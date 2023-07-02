using LanguageExt;

namespace Wavee.UI.Client.Playlist.Models;

public sealed class WaveeUIPlaylist
{
    public required PlaylistRevisionId Revision { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Option<string> ImageId { get; init; }
    public required string? Description { get; init; }
    public required string Owner { get; init; }
    public required bool FromCache { get; init; }
    public required WaveeUIPlaylistTrackInfo[] Tracks { get; init; }
}

public readonly record struct WaveeUIPlaylistTrackInfo(string Id, Option<string> Uid, Option<DateTimeOffset> AddedAt, Option<string> AddedBy, HashMap<string, string> Metadata);
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Features.Playlists.Queries;

public sealed class GetUserPlaylistsQuery : IQuery<GetUserPlaylistsResult>
{

}

public sealed class GetUserPlaylistsResult
{
    public required IReadOnlyCollection<AbsUserPlaylistItem> Items { get; init; }
}

public abstract class AbsUserPlaylistItem
{
    public abstract bool IsFolder { get; }
    public required string Title { get; init; }
    public abstract IReadOnlyCollection<AbsUserPlaylistItem> Items { get; }
}

public sealed class FolderUserPlaylistItem : AbsUserPlaylistItem
{
    public FolderUserPlaylistItem(IReadOnlyCollection<AbsUserPlaylistItem> items)
    {
        Items = items;
    }

    public required string Id { get; init; }
    public override bool IsFolder => true;
    public override IReadOnlyCollection<AbsUserPlaylistItem> Items { get; }
}

public sealed class PlaylistUserPlaylistItem : AbsUserPlaylistItem
{
    public required SpotifyId Id { get; init; }

    public required string Description { get; init; }
    public override bool IsFolder => false;
    public required int Length { get; init; }
    public override IReadOnlyCollection<AbsUserPlaylistItem> Items => Array.Empty<AbsUserPlaylistItem>();
    public required string Owner { get; init; }

    public required IReadOnlyCollection<SpotifyImage> Images { get; init; }
    public required bool HasImages { get; init; }

    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}

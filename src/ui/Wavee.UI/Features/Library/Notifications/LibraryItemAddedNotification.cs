using Mediator;
using Wavee.Spotify.Common;
using Wavee.UI.Domain.Library;

namespace Wavee.UI.Features.Library.Notifications;

public sealed class LibraryItemAddedNotification : INotification
{
    public IEnumerable<(string Id, SpotifyItemType Type, DateTimeOffset AddedAt)> Items { get; init; }
}

public sealed class LibraryItemRemovedNotification : INotification
{
    public required IEnumerable<(string Id, SpotifyItemType Type)> Id { get; init; } = null!;
}
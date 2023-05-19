using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.Models;

public readonly struct SpotifyViewItem
{
    public required AudioId Id { get; init; }
    public required string Title { get; init; }
    public required string Image { get; init; }
    public required string Description { get; init; }
}

public readonly struct SpotifyViewSubItem
{
    public required string Title { get; init; }
    public required AudioId Id { get; init; }
}
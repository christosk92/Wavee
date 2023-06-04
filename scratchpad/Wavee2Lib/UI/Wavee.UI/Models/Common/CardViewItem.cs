using Wavee.Core.Ids;

namespace Wavee.UI.Models.Common;

public sealed class CardViewItem
{
    public AudioId Id { get; init; }
    public string Title { get; init; }
    public string? Subtitle { get; init; }
    public string ImageUrl { get; init; }
}
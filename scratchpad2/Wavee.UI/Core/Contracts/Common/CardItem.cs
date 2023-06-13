using Wavee.Core.Ids;

namespace Wavee.UI.Core.Contracts.Common;

public sealed class CardItem
{
    public required AudioId Id { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
}
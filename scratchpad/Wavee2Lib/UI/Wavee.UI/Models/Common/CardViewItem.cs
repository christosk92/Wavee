using Wavee.Core.Ids;

namespace Wavee.UI.Models.Common;

public sealed class CardViewItem
{
    public AudioId Id { get; set; }
    public string Title { get; set; }
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; }
}
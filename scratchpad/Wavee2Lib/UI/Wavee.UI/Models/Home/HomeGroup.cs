using System.Collections.ObjectModel;
using System.Text.Json;
using Wavee.UI.Models.Common;

namespace Wavee.UI.Models.Home;

public sealed class HomeGroup
{
    public string Id { get; set; }
    public IReadOnlyCollection<CardViewItem> Items { get; set; }
    public string Title { get; set; }
    public string? Subtitle { get; set; }
}
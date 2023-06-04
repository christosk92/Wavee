using System.Text.Json;
using Wavee.UI.Models.Common;

namespace Wavee.UI.Models.Home;

public sealed class HomeGroup
{
    public string Id { get; }
    public IReadOnlyCollection<CardViewItem> Items { get; init; }
    public string Title { get; init; }
    public string? Subtitle { get; init; }
}
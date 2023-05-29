using Wavee.Core.Ids;

namespace Wavee.UI.WinUI.Views.Sidebar.Items;

public class PlaylistSidebarItem : RegularSidebarItem
{
    public required string Owner { get; init; }
    public required AudioId PlaylistId { get; init; }
}
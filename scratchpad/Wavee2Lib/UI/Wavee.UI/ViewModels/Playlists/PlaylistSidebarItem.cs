using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlists;

public sealed class PlaylistSidebarItem : ReactiveObject
{

    public bool IsInFolder { get; set; }
    public string Name { get; set; }
    public ObservableCollection<PlaylistSidebarItem> Items { get; set; }
    public string Id { get; set; }
}
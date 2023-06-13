using System.Windows.Input;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.WinUI.Views.Artist;
using Wavee.UI.WinUI.Views.Playlist;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI;

public static class UICommands
{
    static UICommands()
    {
        NavigateTo = ReactiveCommand.Create((AudioId id) =>
        {
            var pageType = id.Type switch
            {
                AudioItemType.Artist => typeof(ArtistRootView),
                // AudioItemType.Album => typeof(AlbumView),
                AudioItemType.Playlist => typeof(PlaylistView),
                _ => null
            };
            if (pageType is not null)
                SidebarControl.NavigationService.Navigate(pageType, id);
        });
    }

    public static ICommand NavigateTo { get; }
}
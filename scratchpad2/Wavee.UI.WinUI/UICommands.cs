using System.Windows.Input;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.WinUI.Views.Album;
using Wavee.UI.WinUI.Views.Artist;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI;

public static class UICommands
{
    static UICommands()
    {
        NavigateToWithImage = ReactiveCommand.Create((NavigationWithImage nav) =>
        {
            switch (nav.Id.Type)
            {
                case AudioItemType.Album:
                    ShellView.NavigationService.Navigate(typeof(AlbumPage), nav);
                    break;
                case AudioItemType.Playlist:
                    //special
                    break;
            }
        });

        NavigateTo = ReactiveCommand.Create((AudioId id) =>
        {
            switch (id.Type)
            {
                case AudioItemType.Album:
                    ShellView.NavigationService.Navigate(typeof(AlbumPage), id);
                    break;
                case AudioItemType.Artist:
                    ShellView.NavigationService.Navigate(typeof(ArtistPage), id);
                    break;
                case AudioItemType.Playlist:
                    //special
                    break;
            }
        });
    }

    public static ICommand NavigateTo { get; }
    public static ICommand NavigateToWithImage { get; }
}
public readonly record struct NavigationWithImage(AudioId Id, string Image);
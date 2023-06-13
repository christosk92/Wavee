using System.Windows.Input;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.WinUI.Views.Artist;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI;

public static class UICommands
{
    static UICommands()
    {
        NavigateTo = ReactiveCommand.Create((AudioId id) =>
        {
            switch (id.Type)
            {
                case AudioItemType.Album:
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
}
using System.Windows.Input;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.WinUI.Views;
using Wavee.UI.WinUI.Views.Artist;

namespace Wavee.UI.WinUI;

public static class UICommands
{
    static UICommands()
    {
        NavigateTo = ReactiveCommand.Create((AudioId id) =>
        {
            var pageType = id.Type switch
            {
                AudioItemType.Artist => typeof(ArtistView),
                _ => null
            };
            if (pageType is not null)
                ShellView.NavigationService.Navigate(pageType, id);
        });
    }

    public static ICommand NavigateTo { get; }
}
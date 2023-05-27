using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryRootView : UserControl, INavigablePage
{
    public LibraryRootView()
    {
        this.InitializeComponent();
    }

    public bool ShouldKeepInCache(int depth)
    {
        return depth <= 5;
    }

    public Option<INavigableViewModel> ViewModel => Option<INavigableViewModel>.None;
    public void NavigatedTo(object parameter)
    {
        switch (parameter)
        {
            case "songs":
                TopNav.SelectedItem = TopNav.Items[0];
                break;
            case "albums":
                TopNav.SelectedItem = TopNav.Items[1];
                break;
            case "artists":
                TopNav.SelectedItem = TopNav.Items[2];
                break;
        }

        TopNav_OnItemClick(null, null);
    }

    public void RemovedFromCache()
    {
        _librarySongsView?.ViewModel?.Dispose();
        _librarySongsView = null;

        _libaryArtistsView = null;
        _libraryAlbumsView = null;
    }

    private LibrarySongsView? _librarySongsView;
    private LibraryAlbumsView _libraryAlbumsView;
    private LibraryArtistsView _libaryArtistsView;

    private async void TopNav_OnItemClick(object sender, ItemClickEventArgs _)
    {
        await Task.Delay(50);

        var index = TopNav.SelectedIndex;
        var container = (FrameworkElement)TopNav.Items[index];
        var tg = (container).Tag.ToString();
        ShellView.Instance.NavigationService_Navigated(null, (typeof(LibraryRootView), tg));
        MainContent.Content = tg switch
        {
            "songs" => _librarySongsView ??= new LibrarySongsView(),
            "albums" => _libraryAlbumsView ??= new LibraryAlbumsView(),
            "artists" => _libaryArtistsView ??= new LibraryArtistsView(),
        };
    }
}
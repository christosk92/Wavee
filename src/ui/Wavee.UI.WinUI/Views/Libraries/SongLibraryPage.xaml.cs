using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.WinUI.Contracts;


namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class SongLibraryPage : Page, INavigeablePage<LibrarySongsViewModel>
{
    public SongLibraryPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibrarySongsViewModel vm)
        {
            DataContext = vm;
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public LibrarySongsViewModel ViewModel => DataContext is LibrarySongsViewModel vm ? vm : null;
}
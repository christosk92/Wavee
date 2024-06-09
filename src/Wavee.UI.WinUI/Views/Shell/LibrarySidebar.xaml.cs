using System.Windows.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.ViewModels;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class LibrarySidebar : UserControl
{
    public LibrarySidebar()
    {
        this.InitializeComponent();
    }

    public LibraryViewModel ViewModel => (LibraryViewModel)DataContext;

    private void NavigationView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is LibraryCategoryViewModel category)
        {
            var tab = NavigationViewModel.Instance.GetTab(Constants.LibraryTabId);
            if (tab != null)
            {
                tab.ViewModel = category;
                NavigationViewModel.Instance.MakeActive(Constants.LibraryTabId);
            }
        }
        else
        {
            //TODO: (Actual items..)
        }
    }
}
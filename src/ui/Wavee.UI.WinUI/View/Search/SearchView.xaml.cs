using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Search;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.View.Search
{
    public sealed partial class SearchView : UserControl, INavigable
    {
        public SearchView()
        {
            this.InitializeComponent();
        }

        public void NavigatedTo(object parameter)
        {
    
        }

        public SearchBarViewModel ViewModel => ShellViewModel.Instance.SearchBar;
        public void NavigatedFrom(NavigationMode mode)
        {
        }
    }
}

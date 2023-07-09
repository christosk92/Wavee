using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Search;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.View.Search
{
    public sealed partial class SearchView : UserControl, INavigable
    {
        private IDisposable _disposable;
        public SearchView()
        {
            this.InitializeComponent();
        }

        public void NavigatedTo(object parameter)
        {
            _disposable = ViewModel.HasResults
                .Subscribe(x =>
                {
                    NoResults.Visibility =
                        x ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
                });
        }

        public SearchBarViewModel ViewModel => ShellViewModel.Instance.SearchBar;
        public void NavigatedFrom(NavigationMode mode)
        {
            ViewModel.ResetCommand.Execute(null);
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI.ViewModels.Search
{

    [INotifyPropertyChanged]
    public partial class SearchRootViewModel : INavigatable
    {
        private INavigatable? _oldPage;

        public SearchRootViewModel(SearchBarViewModel searchBar)
        {
            SearchBar = searchBar;
        }
        public SearchBarViewModel SearchBar { get; }

        public void ForceShow(bool value)
        {
            if (value)
            {
                if (NavigationService.Instance.Current != this)
                {
                    _oldPage = NavigationService.Instance.Current;
                    NavigationService.Instance.To(this);
                }
            }
            else
            {
                if (_oldPage != null)
                {
                    NavigationService.Instance.To(_oldPage);
                    _oldPage = null;
                }
            }
        }

        public void OnNavigatedTo(object parameter)
        {
            
        }

        public void OnNavigatedFrom()
        {
           
        }

        public int MaxDepth { get; }
    }
}

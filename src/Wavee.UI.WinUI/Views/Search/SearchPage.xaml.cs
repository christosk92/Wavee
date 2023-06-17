using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Search;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Search
{
    public sealed partial class SearchPage : UserControl, INavigable, INotifyPropertyChanged
    {
        private SearchViewModel _viewmodel;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        public SearchViewModel ViewModel
        {
            get => _viewmodel;
            set => SetField(ref _viewmodel, value);
        }
        public void NavigatedTo(object parameter)
        {
            if (parameter is SearchViewModel vm)
            { ViewModel = vm;
            }
        }

        public void NavigatedFrom(NavigationMode mode)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public object Skip(ReadOnlyObservableCollection<GroupedSearchResult> readOnlyObservableCollection, short s)
        {
            return readOnlyObservableCollection.Skip(s);
        }

        public object At(ReadOnlyObservableCollection<GroupedSearchResult> readOnlyObservableCollection, short s)
        {
            return readOnlyObservableCollection.ElementAtOrDefault(s);
        }
    }
}

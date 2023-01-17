// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Search;
using Eum.UI.ViewModels.Search.Sources;
using Eum.UI.WinUI.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Search
{
    public sealed partial class SearchOverviewView : UserControl
    {
        public SearchOverviewView(SearchOverviewViewModel _)
        {
            SearchBar = Ioc.Default.GetRequiredService<MainViewModel>()
                .SearchBar;
            this.InitializeComponent();
            this.DataContext = SearchBar;
        }
        public SearchBarViewModel SearchBar { get; set; }

        private void SearchOverviewView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            SearchBar = null;
        }

        public GridLength GetColumnWidthForSecondColumn(
            ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection)
        {
            return readOnlyObservableCollection.Count > 1 && (
                readOnlyObservableCollection[1] is RecommendationsResultGroup or SongsResultGroup)
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0);
        }

        public object Skip(ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection)
        {
            if (readOnlyObservableCollection.Count > 1 && (
                    readOnlyObservableCollection[1] is RecommendationsResultGroup or SongsResultGroup))
            {
                return readOnlyObservableCollection.Skip(2);
            }

            return readOnlyObservableCollection.Skip(1);
        }

        public bool HasZerothItem(ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection)
        {
            return readOnlyObservableCollection.Count > 0 && readOnlyObservableCollection[0] is TopResultGroup;
        }

        public object IthItemOrNull(ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection, short s)
        {

            return readOnlyObservableCollection.Count > s ? readOnlyObservableCollection[s] : null;
        }

        public bool SecondItemIsRec(ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection)
        {
            return readOnlyObservableCollection.Count > 1 && (
                readOnlyObservableCollection[1] is RecommendationsResultGroup );
        }

        public bool SecondItemIsTrac(ReadOnlyObservableCollection<SearchItemGroup> readOnlyObservableCollection)
        {
            return readOnlyObservableCollection.Count > 1 && (
                readOnlyObservableCollection[1] is SongsResultGroup);
        }
    }
}

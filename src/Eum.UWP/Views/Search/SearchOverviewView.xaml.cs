// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Search;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Search
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
    }
}

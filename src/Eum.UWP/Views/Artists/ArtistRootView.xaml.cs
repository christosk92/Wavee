// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.Logging;
using Eum.UI.ViewModels.Artists;
using Eum.UWP.XamlConverters;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Artists
{
    public sealed partial class ArtistRootView : UserControl
    {
        public ArtistRootView(ArtistRootViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = viewModel;
        }
        public ArtistRootViewModel? ViewModel { get; set; }

        private void GridView_SizeCHanged(object sender, SizeChangedEventArgs e)
        {
            var s = (sender as ListView);

            var columns = Math.Clamp(Math.Floor(s.ActualWidth / 300), 1, 2);
            // if (Math.Abs(columns - 1) < 0.001)
            // {
            //     s.MaxHeight = 5 * ((ItemsWrapGrid) s.ItemsPanelRoot).ItemHeight;
            // }
            // else
            // {
            //     s.MaxHeight = Double.PositiveInfinity;
            // }
            ((ItemsWrapGrid)s.ItemsPanelRoot).ItemWidth = e.NewSize.Width / columns;
        }

        private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel = null;
            this.DataContext = null;
        }
    }

}

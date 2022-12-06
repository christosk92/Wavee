// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Eum.UI.ViewModels.Artists;
using GridView = Microsoft.UI.Xaml.Controls.GridView;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Artists
{
    public sealed partial class ArtistRootView : UserControl
    {
        public ArtistRootView(ArtistRootViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = viewModel;
        }
        public ArtistRootViewModel ViewModel { get; }

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
    }
}

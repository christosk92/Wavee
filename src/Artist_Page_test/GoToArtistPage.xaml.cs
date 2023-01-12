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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Artist_Page_test
{
    public sealed partial class GoToArtistPage : UserControl
    {
        public GoToArtistPage()
        {
            this.InitializeComponent();
        }

        private void GoToArtistTapped(object sender, TappedRoutedEventArgs e)
        {
            GC.Collect();
            (App.MWindow as MainWindow).Wrapper.Content = new ArtistPage();
        }

        private void GoToArtistPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            GC.Collect();
        }
    }
}

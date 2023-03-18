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
using Wavee.UI.ViewModels.Libray;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Library
{
    public sealed partial class LibrarySongsView : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(LibrarySongsViewModel), typeof(LibrarySongsView), new PropertyMetadata(default(LibrarySongsViewModel)));

        public LibrarySongsView()
        {
            this.InitializeComponent();
        }

        public LibrarySongsViewModel ViewModel
        {
            get => (LibrarySongsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private void AscendingTapped(object sender, TappedRoutedEventArgs e)
        {
            if (AscendingBox.IsChecked == true)
            {
                ViewModel.SortAscending = true;
            }
            else
            {
                ViewModel.SortAscending = false;
            }
        }

        private void ExtendSortLv_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var firstItem = e.AddedItems.Cast<string>().First();
                ViewModel.SortBy = firstItem;
            }
        }
    }
}

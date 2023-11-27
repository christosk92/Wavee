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
using Wavee.UI.Features.Identity.Entities;
using Wavee.UI.Features.MainWindow;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Main
{
    public sealed partial class MainContent : UserControl
    {
        public MainContent()
        {
            this.InitializeComponent();
        }

        public MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
        public bool IsNotNull(object? x) => x is not null;

        private async void MainContent_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Identity.Initialize();
        }

        public Visibility IsNotNullThenVisible(object? obj)
        {
            return obj is not null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

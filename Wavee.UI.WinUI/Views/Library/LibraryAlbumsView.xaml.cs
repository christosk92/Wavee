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
    public sealed partial class LibraryAlbumsView : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(LibraryAlbumsViewModel), typeof(LibraryAlbumsView), new PropertyMetadata(default(LibraryAlbumsViewModel)));

        public LibraryAlbumsView()
        {
            this.InitializeComponent();
        }

        public LibraryAlbumsViewModel ViewModel
        {
            get => (LibraryAlbumsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}

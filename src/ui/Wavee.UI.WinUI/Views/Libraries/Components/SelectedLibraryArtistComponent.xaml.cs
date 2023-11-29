using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels.Artist;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Libraries.Components
{
    public sealed partial class SelectedLibraryArtistComponent : UserControl
    {
        public static readonly DependencyProperty SelectedArtistProperty = DependencyProperty.Register(nameof(SelectedArtist), typeof(LibraryArtistViewModel), typeof(SelectedLibraryArtistComponent), new PropertyMetadata(default(LibraryArtistViewModel)));

        public SelectedLibraryArtistComponent()
        {
            this.InitializeComponent();
        }

        public LibraryArtistViewModel SelectedArtist
        {
            get => (LibraryArtistViewModel)GetValue(SelectedArtistProperty);
            set => SetValue(SelectedArtistProperty, value);
        }
    }
}

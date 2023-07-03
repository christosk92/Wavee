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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Playlist
{
    public sealed partial class PlaylistSortButtons : UserControl
    {
        public static readonly DependencyProperty ShowArtistsProperty = DependencyProperty.Register(nameof(ShowArtists), typeof(bool), typeof(PlaylistSortButtons), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty ShowAlbumProperty = DependencyProperty.Register(nameof(ShowAlbum), typeof(bool), typeof(PlaylistSortButtons), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty ShowDateProperty = DependencyProperty.Register(nameof(ShowDate), typeof(bool), typeof(PlaylistSortButtons), new PropertyMetadata(default(bool)));

        public PlaylistSortButtons()
        {
            this.InitializeComponent();
        }

        public bool ShowArtists
        {
            get => (bool)GetValue(ShowArtistsProperty);
            set => SetValue(ShowArtistsProperty, value);
        }

        public bool ShowAlbum
        {
            get => (bool)GetValue(ShowAlbumProperty);
            set => SetValue(ShowAlbumProperty, value);
        }

        public bool ShowDate
        {
            get => (bool)GetValue(ShowDateProperty);
            set => SetValue(ShowDateProperty, value);
        }
    }
}

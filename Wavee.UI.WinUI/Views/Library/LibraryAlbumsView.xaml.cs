using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Track;
using Wavee.UI.Models.TrackSources;

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

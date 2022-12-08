// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Playlists;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Controls
{
    public sealed partial class PlaylistTrackView : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(PlaylistTrackViewModel), typeof(PlaylistTrackView), new PropertyMetadata(default(PlaylistTrackViewModel)));
        // public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(PlaylistTrackViewModel), typeof(PlaylistTrackView), new PropertyMetadata(default(PlaylistTrackViewModel)));

        public PlaylistTrackView()
        {
            this.InitializeComponent();
        }

        // public PlaylistTrackViewModel ViewModel
        // {
        //     get => (PlaylistTrackViewModel) GetValue(ViewModelProperty);
        //     set => SetValue(ViewModelProperty, value);
        // }
        public PlaylistTrackViewModel ViewModel
        {
            get => (PlaylistTrackViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}

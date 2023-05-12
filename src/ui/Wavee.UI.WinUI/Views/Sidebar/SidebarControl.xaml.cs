using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.UI.Bases;
using Wavee.UI.Helpers;
using Wavee.UI.ViewModels.Playlist;
using Wavee.UI.ViewModels.Sidebar;

namespace Wavee.UI.WinUI.Views.Sidebar
{
    public sealed partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty SidebarWidthProperty = DependencyProperty.Register(nameof(SidebarWidth), typeof(double), typeof(SidebarControl),
            new PropertyMetadata(Constants.DefaultSidebarWidth));

        public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems),
            typeof(IReadOnlyCollection<AbsSidebarItemViewModel>), typeof(SidebarControl), new PropertyMetadata(default(IReadOnlyCollection<AbsSidebarItemViewModel>)));

        public static readonly DependencyProperty PlaylistsProperty = DependencyProperty.Register(nameof(Playlists), typeof(ReadOnlyObservableCollection<PlaylistViewModel>), typeof(SidebarControl), new PropertyMetadata(default(ReadOnlyObservableCollection<PlaylistViewModel>)));


        public SidebarControl()
        {
            this.InitializeComponent();
            SidebarResizer.Minimum = Constants.MinimumSidebarWidth;
            SidebarResizer.Maximum = Constants.MaximumSidebarWidth;
        }
        private double SidebarWidth
        {
            get => (double)GetValue(SidebarWidthProperty);
            set => SetValue(SidebarWidthProperty, value);
        }
        public IObservable<double> SidebarWidthChanged => this.WhenAnyValue(x => x.SidebarWidth);

        public IReadOnlyCollection<AbsSidebarItemViewModel> SidebarItems
        {
            get => (IReadOnlyCollection<AbsSidebarItemViewModel>)GetValue(SidebarItemsProperty);
            set => SetValue(SidebarItemsProperty, value);
        }

        public ReadOnlyObservableCollection<PlaylistViewModel> Playlists
        {
            get => (ReadOnlyObservableCollection<PlaylistViewModel>)GetValue(PlaylistsProperty);
            set => SetValue(PlaylistsProperty, value);
        }

        public void SetSidebarWidth(double width)
        {
            CoreSplitView.OpenPaneLength = width;
        }
        private void SplitViewPaneContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SidebarWidth = e.NewSize.Width;
        }

        private void FixedSidebarItemsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlaylistsListView.SelectedIndex = -1;
        }

        private void PlaylistsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FixedSidebarItemsListView.SelectedIndex = -1;
        }

        private void FixedSidebarItemsListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in FixedSidebarItemsListView.Items)
            {
                var container = (ListViewItem)FixedSidebarItemsListView.ContainerFromItem(item);
                //if item is header, set it to not selectable
                if (item is HeaderSidebarItem)
                {
                    container.IsHitTestVisible = false;
                }
            }
        }
    }
}

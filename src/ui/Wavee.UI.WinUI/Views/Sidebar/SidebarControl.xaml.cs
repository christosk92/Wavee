using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.WinUI.UI;
using DynamicData;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using Wavee.UI.Bases;
using Wavee.UI.Helpers;
using Wavee.UI.Users;
using Wavee.UI.ViewModels;
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
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(User), typeof(SidebarControl), new PropertyMetadata(default(User)));
        public static readonly DependencyProperty PlaylistFiltersProperty = DependencyProperty.Register(nameof(PlaylistFilters), typeof(Seq<PlaylistSourceFilter>), typeof(SidebarControl),
            new PropertyMetadata(default(Seq<PlaylistSourceFilter>), PlaylistFiltersChanged));

        public static readonly DependencyProperty PlaylistSortProperty = DependencyProperty.Register(
            nameof(PlaylistSort), typeof(PlaylistSortProperty), typeof(SidebarControl)
            , new PropertyMetadata(default(PlaylistSortProperty), SortChanged));

        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register(nameof(NavigationFrame), typeof(object), typeof(SidebarControl), new PropertyMetadata(default(object)));

        private static void SortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var k = d as SidebarControl;
            if (e.NewValue is null) return;
            var s = (PlaylistSortProperty)e.NewValue;
            k.PlaylistSortComboBox.SelectedIndex = s switch
            {
                ViewModels.PlaylistSortProperty.Custom => 0,
                ViewModels.PlaylistSortProperty.Created => 1,
                ViewModels.PlaylistSortProperty.Alphabetical => 2,
                ViewModels.PlaylistSortProperty.RecentlyPlayed => 3,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static void PlaylistFiltersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is null) return;
            var k = d as SidebarControl;
            var items = new List<object>();
            foreach (var item in (Seq<PlaylistSourceFilter>)e.NewValue)
            {
                switch (item)
                {
                    case PlaylistSourceFilter.Yours:
                        items.Add(k.PlaylistFilterTokenView.Items[0]);
                        break;
                    case PlaylistSourceFilter.Others:
                        items.Add(k.PlaylistFilterTokenView.Items[1]);
                        break;
                    case PlaylistSourceFilter.Spotify:
                        items.Add(k.PlaylistFilterTokenView.Items[2]);
                        break;
                    case PlaylistSourceFilter.Local:
                        items.Add(k.PlaylistFilterTokenView.Items[3]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            k.PlaylistFilterTokenView.SelectedItems.AddRange(items);
        }

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

        public User User
        {
            get => (User)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public Seq<PlaylistSourceFilter> PlaylistFilters
        {
            get => (Seq<PlaylistSourceFilter>)GetValue(PlaylistFiltersProperty);
            set => SetValue(PlaylistFiltersProperty, value);
        }

        public PlaylistSortProperty PlaylistSort
        {
            get => (PlaylistSortProperty)GetValue(PlaylistSortProperty);
            set => SetValue(PlaylistSortProperty, value);
        }

        public object NavigationFrame
        {
            get => (object)GetValue(NavigationFrameProperty);
            set => SetValue(NavigationFrameProperty, value);
        }

        public event EventHandler? OnCreatePlaylistRequested;

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

        public string GetDisplayName(User user)
        {
            return user?.DispayName ?? "Guest";
        }

        public string ParseProductString(User user)
        {
            return user?.ProductType switch
            {
                UserProductType.Local => "Local",
                UserProductType.SpotifyPremium => "Spotify Premium",
                _ => "Guest",
            };
        }

        private async void PlaylistFilterTokenView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            await Task.Delay(30);
            var selectedItems = PlaylistFilterTokenView.SelectedItems.Where(c => c is not null).ToArr();
            var items = new PlaylistSourceFilter[selectedItems.Count];
            for (var index = 0; index < selectedItems.Count; index++)
            {
                var selectedItem = (TokenItem)selectedItems[index];
                items[index] = selectedItem.Tag switch
                {
                    "yours" => PlaylistSourceFilter.Yours,
                    "others" => PlaylistSourceFilter.Others,
                    "spotify" => PlaylistSourceFilter.Spotify,
                    "local" => PlaylistSourceFilter.Local,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            PlaylistFilters = new Seq<PlaylistSourceFilter>(items);
        }

        private void PlaylistSortComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (ComboBoxItem)((sender as ComboBox)).SelectedItem;
            PlaylistSort = selectedItem.Tag switch
            {
                "alph" => ViewModels.PlaylistSortProperty.Alphabetical,
                "created" => ViewModels.PlaylistSortProperty.Created,
                "recent" => ViewModels.PlaylistSortProperty.RecentlyPlayed,
                "custom" => ViewModels.PlaylistSortProperty.Custom,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void CreatePLaylistTapped(object sender, TappedRoutedEventArgs e)
        {
            OnCreatePlaylistRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

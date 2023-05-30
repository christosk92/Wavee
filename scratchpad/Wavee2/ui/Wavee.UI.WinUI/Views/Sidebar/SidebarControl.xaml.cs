using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Wavee.UI.ViewModels.Playlist;
using Wavee.UI.WinUI.Views.Sidebar.Items;
using Windows.Media.Playlists;
using DynamicData.Binding;
using Wavee.UI.ViewModels;
using Microsoft.UI.Xaml.Input;
using System.Windows.Navigation;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Wavee.UI.WinUI.Views.Sidebar
{
    public sealed partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty SidebarWidthProperty = DependencyProperty.Register(nameof(SidebarWidth), typeof(double), typeof(SidebarControl), new PropertyMetadata(200));
        public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems), typeof(IReadOnlyCollection<AbsSidebarItemViewModel>), typeof(SidebarControl), new PropertyMetadata(default(IReadOnlyCollection<AbsSidebarItemViewModel>)));
        public static readonly DependencyProperty PlaylistsProperty = DependencyProperty.Register(nameof(Playlists),
            typeof(ObservableCollectionExtended<PlaylistViewModel>), typeof(SidebarControl), new PropertyMetadata(default(ObservableCollectionExtended<PlaylistViewModel>)));
        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register(nameof(NavigationFrame), typeof(object),
        typeof(SidebarControl), new PropertyMetadata(default(object)));

        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(User), typeof(SidebarControl), new PropertyMetadata(default(User)));

        public SidebarControl()
        {
            this.InitializeComponent();
        }
        public IReadOnlyCollection<AbsSidebarItemViewModel> SidebarItems
        {
            get => (IReadOnlyCollection<AbsSidebarItemViewModel>)GetValue(SidebarItemsProperty);
            set => SetValue(SidebarItemsProperty, value);
        }

        private double SidebarWidth
        {
            get => (double)GetValue(SidebarWidthProperty);
            set => SetValue(SidebarWidthProperty, value);
        }
        public object NavigationFrame
        {
            get => (object)GetValue(NavigationFrameProperty);
            set => SetValue(NavigationFrameProperty, value);
        }
        public User User
        {
            get => (User)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public ObservableCollectionExtended<PlaylistViewModel> Playlists
        {
            get => (ObservableCollectionExtended<PlaylistViewModel>)GetValue(PlaylistsProperty);
            set => SetValue(PlaylistsProperty, value);
        }

        private void SplitViewPaneContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SidebarWidth = e.NewSize.Width;
        }

        private void FixedSidebarItemsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlaylistsListView.SelectedItem = null;
        }
        private void FixedSidebarItemsListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var c = e.ClickedItem;
            //NavigateTo(c);
        }
        private void PlaylistsListView_OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            FixedSidebarItemsListView.SelectedIndex = -1;
            // NavigateTo(args.InvokedItem);
        }
        public Visibility HideIfNonZero(int i)
        {
            return i > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AddPlaylistButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // NavigationService.Instance.Navigate(typeof(CreatePlaylistView));
        }
        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //ShellView.NavigationService.Navigate(typeof(SettingsView));
        }
        public string GetInitials(User user)
        {
            //display name or id
            return user
                .DisplayName
                .Match(s => s, () => user.Id.ToString())
                .Substring(0, 2);
        }

        public string GetDisplayName(User option)
        {
            return option.DisplayName.Match(s => s, () => option.Id.ToString());
        }

        public string GetProduct(User user)
        {
            return user
                .Metadata
                .Find("product")
                .Match(s => s, () => "Spotify");
        }

        public ImageSource GetProfilePicture(User user)
        {
            if (user.ImageId.IsSome)
            {
                var imageId = user.ImageId.ValueUnsafe();
                var url = new Uri(imageId);
                return new BitmapImage(url);
            }

            return null;
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
                else if (item is RegularSidebarItem r && r.ForceDisable)
                {
                    container.IsEnabled = false;
                    container.IsHitTestVisible = false;
                }
            }
        }
    }
}

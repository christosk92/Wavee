using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ReactiveUI;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Models;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Playlist;
using Wavee.UI.WinUI.Views.Sidebar.Items;

namespace Wavee.UI.WinUI.Views.Sidebar
{
    public sealed partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty SidebarWidthProperty = DependencyProperty.Register(nameof(SidebarWidth), typeof(double), typeof(SidebarControl), new PropertyMetadata(Constants.DefaultSidebarWidth));
        public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems), typeof(IReadOnlyCollection<AbsSidebarItemViewModel>), typeof(SidebarControl), new PropertyMetadata(default(IReadOnlyCollection<AbsSidebarItemViewModel>)));
        public static readonly DependencyProperty PlaylistsProperty = DependencyProperty.Register(nameof(Playlists), typeof(ReadOnlyObservableCollection<PlaylistInfo>), typeof(SidebarControl), new PropertyMetadata(default(ReadOnlyObservableCollection<PlaylistInfo>)));
        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register(nameof(NavigationFrame), typeof(object),
            typeof(SidebarControl), new PropertyMetadata(default(object)));

        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(User), typeof(SidebarControl), new PropertyMetadata(default(User)));

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

        public ReadOnlyObservableCollection<PlaylistInfo> Playlists
        {
            get => (ReadOnlyObservableCollection<PlaylistInfo>)GetValue(PlaylistsProperty);
            set => SetValue(PlaylistsProperty, value);
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
            PlaylistsListView.SelectedItem = null;
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0];
                NavigateTo(item);
            }
        }
        private void PlaylistsListView_OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            FixedSidebarItemsListView.SelectedIndex = -1;
            NavigateTo(args.InvokedItem);
        }

        private static void NavigateTo(object item)
        {
            var pageType = item switch
            {
                RegularSidebarItem re => re.Slug switch
                {
                    "home" => typeof(HomeRootView),
                    _ => null
                },
                _ => null
            };
            if (pageType is not null)
                ShellView.NavigationService.Navigate(pageType);
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

        public Visibility HideIfNonZero(int i)
        {
            return i > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AddPlaylistButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            NavigationService.Instance.Navigate(typeof(CreatePlaylistView));
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
    }
}

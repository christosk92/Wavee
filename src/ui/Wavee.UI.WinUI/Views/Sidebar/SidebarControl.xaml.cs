using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Models;
using Wavee.UI.ViewModels;
using Wavee.UI.ViewModels.Playlists;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Library;
using Wavee.UI.WinUI.Views.Playlist;
using Wavee.UI.WinUI.Views.Settings;
using Wavee.UI.WinUI.Views.Sidebar.Items;

namespace Wavee.UI.WinUI.Views.Sidebar
{
    public sealed partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty SidebarWidthProperty = DependencyProperty.Register(nameof(SidebarWidth), typeof(double), typeof(SidebarControl), new PropertyMetadata(Constants.DefaultSidebarWidth));
        public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems), typeof(IReadOnlyCollection<AbsSidebarItemViewModel>), typeof(SidebarControl), new PropertyMetadata(default(IReadOnlyCollection<AbsSidebarItemViewModel>)));
        public static readonly DependencyProperty PlaylistsProperty = DependencyProperty.Register(nameof(Playlists), typeof(ReadOnlyObservableCollection<PlaylistSubscription>), typeof(SidebarControl), new PropertyMetadata(default(ReadOnlyObservableCollection<PlaylistSubscription>)));
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

        public ReadOnlyObservableCollection<PlaylistSubscription> Playlists
        {
            get => (ReadOnlyObservableCollection<PlaylistSubscription>)GetValue(PlaylistsProperty);
            set => SetValue(PlaylistsProperty, value);
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
            // if (e.AddedItems.Count > 0)
            // {
            //     var item = e.AddedItems[0];
            //     NavigateTo(item);
            // }
        }
        private async void FixedSidebarItemsListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var c = e.ClickedItem;
            NavigateTo(c);
        }
        private void PlaylistsListView_OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            FixedSidebarItemsListView.SelectedIndex = -1;
            NavigateTo(args.InvokedItem);
        }

        private static Dictionary<string, (Type, object)> _navigationMapping
         = new()
         {
            { "home",(typeof(HomeRootView), null)},
            {"songs", (typeof(LibraryRootView), "songs")},
            {"albums", (typeof(LibraryRootView), "albums")},
            {"artists", (typeof(LibraryRootView), "artists")},
         };
        private void NavigateTo(object item)
        {
            if (item is PlaylistSubscription { IsFolder: false } pls)
            {
                ShellView.NavigationService.Navigate(typeof(PlaylistView), pls);
                return;
            }
            else if (item is PlaylistSubscription { IsFolder: true } folder)
            {
                //go to folder? or just expand
                return;
            }
            var slug = item switch
            {
                RegularSidebarItem re => re.Slug,
                _ => null
            };
            if (slug is null)
                return;

            if (!_navigationMapping.TryGetValue(slug, out var pageType))
                return;
            // var pageType = item switch
            // {
            //     RegularSidebarItem re => re.Slug switch
            //     {
            //         "home" => typeof(HomeRootView),
            //         _ => null
            //     },
            //     _ => null
            // };
            if (pageType.Item1 is not null)
                ShellView.NavigationService.Navigate(pageType.Item1, pageType.Item2);
        }

        public void SetSelected(Type type, object param)
        {
            var slugAssociated = _navigationMapping.SingleOrDefault(x => x.Value.Item1 == type
                                                                         && (x.Value.Item2 == param || x.Value.Item2?.ToString() == param?.ToString()));
            if (slugAssociated.Value.Item1 is not null)
            {
                var item = SidebarItems.SingleOrDefault(x =>
                    x is RegularSidebarItem f && f.Slug == slugAssociated.Key);
                //TODO: Playlists
                if (item is not null)
                {
                    FixedSidebarItemsListView.SelectedItem = item;
                }
                else
                {
                    FixedSidebarItemsListView.SelectedIndex = -1;
                    PlaylistsListView.SelectedItem = -1;
                }
            }
            else
            {
                FixedSidebarItemsListView.SelectedIndex = -1;
                PlaylistsListView.SelectedItem = -1;
            }
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
                else if (item is RegularSidebarItem r && r.Slug is "podcasts")
                {
                    container.IsEnabled = false;
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

        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShellView.NavigationService.Navigate(typeof(SettingsView));
        }
    }
}

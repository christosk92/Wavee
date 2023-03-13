﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Spotify.Metadata;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Playback.Contexts.Local;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.WinUI.Behaviors
{
    public class ListViewBaseContextMenuBehavior : BehaviorBase<ListViewBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.RightTapped += AssociatedObjectOnRightTapped;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RightTapped -= AssociatedObjectOnRightTapped;
        }

        private void AssociatedObjectOnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            void ShowSingle(object datacontext, int offset)
            {
                var flyout = CreateSingleItemContextMenu(datacontext,
                    LocalLibraryContext.Create(PlayOrderType.Imported,
                        true,
                        offset,
                        LibraryViewType.Albums));
                var tappedItem = (UIElement)e.OriginalSource;
                flyout.ShowAt(tappedItem, e.GetPosition(tappedItem));
            }

            void ShowMultiple(IList<object> datacontexts)
            {
                var flyout = CreateMultipleItemsContextMenu(datacontexts);
                var tappedItem = (UIElement)e.OriginalSource;
                flyout.ShowAt(tappedItem, e.GetPosition(tappedItem));
            }
            var item = e.OriginalSource;
            if (item is FrameworkElement { DataContext: { } } f)
            {
                if (!AssociatedObject.SelectedItems.Contains(f.DataContext))
                {
                    var index = AssociatedObject.Items.IndexOf(f.DataContext);
                    if (index != -1)
                    {
                        AssociatedObject.SelectedItems.Clear();
                        AssociatedObject.SelectRange(new ItemIndexRange(index, 1));
                    }
                    else
                    {
                        return;
                    }
                }
            }

            switch (AssociatedObject.SelectedItems.Count)
            {
                case 0:
                    break;
                case 1:
                    var firstItem = AssociatedObject.SelectedItems.First();
                    var indexOf = AssociatedObject.Items.IndexOf(firstItem);
                    // var moreItems = AssociatedObject.Items.Skip(indexOf + 1);
                    ShowSingle(firstItem, indexOf);
                    break;
                default:
                    ShowMultiple(AssociatedObject.SelectedItems);
                    break;
            }
        }

        private static MenuFlyout CreateSingleItemContextMenu(object datacontext, IPlayContext? playcontext = null)
        {
            var contextMenu = new MenuFlyout();
            if (datacontext is IDescribeable desc)
            {
                contextMenu.Items.Add(new MenuFlyoutItem
                {
                    Text = desc.Describe(),
                    IsEnabled = false,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            if (playcontext is not null)
            {
                var playItem = new MenuFlyoutItem
                {
                    Text = "Play",
                    Command = PlayerViewModel.PlayCommand,
                    CommandParameter = playcontext,
                    // CommandParameter = new[]
                    // {
                    //     playableItem
                    //
                    // }.Concat(moreItems.Select(a => (IPlayableItem?)a).Where(a => a is not null))
                    //     .ToArray(),
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE102"
                    },
                };
                var playNextItem = new MenuFlyoutItem
                {
                    Text = "Play next",
                    Command = PlayerViewModel.PlayNextCommand,
                    // CommandParameter = new[]
                    // {
                    //     playableItem
                    // },
                    Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"), Glyph = "\uF5EB" }
                };
                contextMenu.Items.Add(playItem);
                contextMenu.Items.Add(playNextItem);
                contextMenu.Items.Add(new MenuFlyoutSeparator());
            }

            if (datacontext is IAddableItem addableItem)
            {
                var addToItem = new MenuFlyoutSubItem()
                {
                    Text = "Add to",
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE109"
                    },
                    Items = { }
                };
                contextMenu.Items.Add(addToItem);
            }

            if (datacontext is IEditableItem { CanEdit: true })
            {
                var editInfoItem = new MenuFlyoutItem
                {
                    Text = "Edit info",
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE104"
                    },
                };
                contextMenu.Items.Add(editInfoItem);
            }

            if (datacontext is AlbumViewModel)
            {

                var showAlbumItem = new MenuFlyoutItem
                {
                    Text = "Show album",
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE93C"
                    }
                };
                var showArtistItem = new MenuFlyoutItem
                {
                    Text = "Show artist",
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uEBDA"
                    },
                };
                contextMenu.Items.Add(showAlbumItem);
                contextMenu.Items.Add(showArtistItem);
            }

            return contextMenu;
        }

        private static MenuFlyout CreateMultipleItemsContextMenu(IList<object> datacontexts, IPlayContext? playcontext = null)
        {
            var contextMenu = new MenuFlyout();
            contextMenu.Items.Add(new MenuFlyoutItem
            {
                Text = $"{datacontexts.Count} item{(datacontexts.Count == 1 ? string.Empty : "s")}",
                IsEnabled = false,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            if (playcontext is not null)
            {
                var playItem = new MenuFlyoutItem
                {
                    Text = "Play",
                    Command = PlayerViewModel.PlayCommand,
                    CommandParameter = playcontext,
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE102"
                    },
                };
                var playNextItem = new MenuFlyoutItem
                {
                    Text = "Play next",
                    Command = PlayerViewModel.PlayNextCommand,
                    // CommandParameter = datacontexts,
                    Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"), Glyph = "\uF5EB" }
                };
                contextMenu.Items.Add(playItem);
                contextMenu.Items.Add(playNextItem);
                contextMenu.Items.Add(new MenuFlyoutSeparator());
            }
            if (datacontexts.All(a => a is IAddableItem))
            {
                var addToItem = new MenuFlyoutSubItem()
                {
                    Text = "Add to",
                    Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "\uE109"
                    },
                    Items = { }
                };
                contextMenu.Items.Add(addToItem);
            }

            return contextMenu;
        }

    }
}
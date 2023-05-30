using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Playlist
{
    public sealed partial class PlaylistView : UserControl, INavigablePage
    {
        public PlaylistView()
        {
            this.InitializeComponent();
        }

        public bool ShouldKeepInCache(int depth)
        {
            //only if depth leq 3
            return depth <= 3;
        }

        Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

        public async void NavigatedTo(object parameter)
        {
            //ok, so what?
            if (parameter is PlaylistViewModel vr)
            {
                ViewModel = vr;
                await ViewModel.SetupForUI();
                //await Task.Delay(10);
                this.Bindings.Update();
            }
            else if (parameter is AudioId id)
            {
                //TODO: fetch the playlist
            }
        }

        public void RemovedFromCache()
        {
            //cleanup
            ViewModel?.DestroyForUI();
        }

        public PlaylistViewModel ViewModel { get; set; }

        private void PlaylistView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
            // //ratio is around 1:1, so 1/2
            // if (!string.IsNullOrEmpty(ViewModel.Playlist?.LargeImage))
            // {
            //     var topHeight = newSize * 0.5;
            //     topHeight = Math.Min(topHeight, 550);
            //     LargeImage.Height = topHeight;
            // }
            // else
            // {
            //     //else its only 1/4th
            //     var topHeight = newSize * 0.25;
            //     topHeight = Math.Min(topHeight, 550);
            //     LargeImage.Height = topHeight;
            // }
        }

        private async void PlaylistView_OnLoaded(object sender, RoutedEventArgs e)
        {
            //     await ViewModel.PlaylistFetched.Task;
            //     this.Bindings.Update();
            //
            //     RegularCaption.Text = "Playlist";
            //     MetadataPnale.Visibility = Visibility.Visible;
            //     if (!string.IsNullOrEmpty(ViewModel.Playlist.LargeImage))
            //     {
            //         ShowPanelAnim.Start();
            //         LargeImage.Visibility = Visibility.Visible;
            //         SmallPlaylist.Visibility = Visibility.Collapsed;
            //     }
            //     else
            //     {
            //         LargeImage.Visibility = Visibility.Collapsed;
            //         SmallPlaylist.Visibility = Visibility.Visible;
            //     }
            //
            //     PlaylistView_OnSizeChanged(this, null);
        }

        public object SavedToContent(bool b)
        {
            var stckp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            if (b)
            {
                stckp.Children.Add(new FontIcon
                {
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    Glyph = "\uEB52"
                });
                stckp.Children.Add(new TextBlock
                {
                    Text = "Remove"
                });
            }
            else
            {
                stckp.Children.Add(new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE006" });
                stckp.Children.Add(new TextBlock { Text = "Save" });
            }

            return stckp;
        }

        private void SortButtonTapp(object sender, TappedRoutedEventArgs e)
        {

        }

        public PlaylistTrackSortType NextSortType(PlaylistTrackSortType playlistTrackSortType, string forWhat)
        {
            return PlaylistTrackSortType.IndexAsc;
        }

        private void NavigateToMetadata(object sender, TappedRoutedEventArgs e)
        {
            var hp = (sender as HyperlinkButton).Tag;
            if (hp is AudioId id)
            {
                UICommands.NavigateTo.Execute(id);
            }
        }

        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            //calculate next page
            var sv = sender as ScrollViewer;

        }
    }

    public enum PlaylistTrackSortType
    {
        IndexAsc
    }
}

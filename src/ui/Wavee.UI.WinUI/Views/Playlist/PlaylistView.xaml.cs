using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Playlist
{
    public sealed partial class PlaylistView : UserControl, INavigablePage
    {
        public PlaylistView()
        {
            ViewModel = new PlaylistViewModel<WaveeUIRuntime>(App.Runtime);
            this.InitializeComponent();
        }

        public bool ShouldKeepInCache(int depth)
        {
            //only if depth leq 3
            return depth <= 3;
        }

        Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

        public void NavigatedTo(object parameter)
        {
            //ok, so what?
        }

        public void RemovedFromCache()
        {
            //cleanup
            ViewModel.Clear();
        }

        public PlaylistViewModel<WaveeUIRuntime> ViewModel { get; }

        private void PlaylistView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
            //ratio is around 1:1, so 1/2
            if (!string.IsNullOrEmpty(ViewModel.Playlist?.LargeImage))
            {
                var topHeight = newSize * 0.5;
                topHeight = Math.Min(topHeight, 550);
                LargeImage.Height = topHeight;
            }
            else
            {
                //else its only 1/4th
                var topHeight = newSize * 0.25;
                topHeight = Math.Min(topHeight, 550);
                LargeImage.Height = topHeight;
            }
        }

        private async void PlaylistView_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.PlaylistFetched.Task;
            MetadataPnale.Visibility = Visibility.Visible;
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
    }
}

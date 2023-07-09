using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Wavee.Id;
using Wavee.UI.Client.Album;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Album;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.View.Artist;

namespace Wavee.UI.WinUI.View.Album
{
    public sealed partial class AlbumView : UserControl, INavigable, ICacheablePage
    {
        public AlbumView()
        {
            ViewModel = new AlbumViewModel(ShellViewModel.Instance.User);
            this.InitializeComponent();
        }

        private void AlbumView_OnActualThemeChanged(FrameworkElement sender, object args)
        {
            ViewModel.OnThemeChange(this.ActualTheme switch
            {
                ElementTheme.Dark => AppTheme.Dark,
                ElementTheme.Light => AppTheme.Light,
            });
        }

        public AlbumViewModel ViewModel { get; }
        private bool _traveledWithImage;

        public async void NavigatedTo(object parameter)
        {
            if (parameter is string id)
            {
                await ViewModel.Fetch(id);
                ViewModel.OnThemeChange(this.ActualTheme switch
                {
                    ElementTheme.Dark => AppTheme.Dark,
                    ElementTheme.Light => AppTheme.Light,
                });
            }
            else if (parameter is NavigatingWithImage img)
            {
                _traveledWithImage = true;

                AlbumImage.Source = img.Image;
                ViewModel.SetImage = true;
                await ViewModel.Fetch(img.Id);
                ViewModel.OnThemeChange(this.ActualTheme switch
                {
                    ElementTheme.Dark => AppTheme.Dark,
                    ElementTheme.Light => AppTheme.Light,
                });
            }
        }

        public void NavigatedFrom(NavigationMode mode)
        {
        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 2;
        }

        public void RemovedFromCache()
        {
        }

        public object? GetCorrectViewSource(WaveeUIAlbumDisc[] waveeUiAlbumDiscs)
        {
            if (waveeUiAlbumDiscs is
                {
                    Length: > 1
                })
            {
                var cvs = new CollectionViewSource
                {
                    IsSourceGrouped = true,
                    Source = waveeUiAlbumDiscs,
                    ItemsPath = new PropertyPath(nameof(WaveeUIAlbumDisc.Tracks))
                };
                return cvs.View;
            }

            if (waveeUiAlbumDiscs is
                {
                    Length: 1
                })
            {
                var cvs = new CollectionViewSource
                {
                    IsSourceGrouped = false,
                    Source = waveeUiAlbumDiscs[0].Tracks,
                };

                return cvs.View;
            }

            return null;
        }

        private void AlbumArtistTapped(object sender, TappedRoutedEventArgs e)
        {
            var id = (SpotifyId)((FrameworkElement)sender).Tag;
            NavigationService.Instance.Navigate(typeof(ArtistView), id.ToString());
        }

      
        private async void AlbumName_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleFactor = 0.05; // Adjust as needed to control how much the text scales with the control's size
            double newFontSize = this.ActualWidth * scaleFactor;
            if (AlbumNameTextBlock.IsTextTrimmed)
            {
                // Set a minimum and maximum font size
                newFontSize = Math.Max(10, newFontSize); // Minimum size of 10
                newFontSize = Math.Min(58, newFontSize); // Maximum size of 58
                AlbumNameTextBlock.FontSize = newFontSize;
                await Task.Delay(200);
            }
        }
    }
}
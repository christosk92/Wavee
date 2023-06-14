using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Album;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Album
{
    public sealed partial class AlbumPage : UserControl, INavigable
    {
        public AlbumPage()
        {
            this.InitializeComponent();
            ViewModel = new AlbumViewModel();
        }

        public AlbumViewModel ViewModel { get; }

        public async void NavigatedTo(object parameter)
        {
            if (parameter is NavigationWithImage img)
            {
                // var bmp = new BitmapImage();
                // Src.Source = bmp;
                // bmp.UriSource = new Uri(img.Image, UriKind.RelativeOrAbsolute);
                ViewModel.AlbumImage = img.Image;
                var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
                anim.Configuration = new DirectConnectedAnimationConfiguration();
                if (anim != null)
                {
                    anim.TryStart(Src);
                }
            }
        }

        public void NavigatedFrom(NavigationMode mode)
        {
            if (mode is NavigationMode.Back)
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", Src);
        }

        private void TracksList_OnLoaded(object sender, RoutedEventArgs e)
        {
            var scroller = ((ListView)sender).FindDescendant<ScrollViewer>();

            scroller.Unloaded += ScrollerOnUnloaded;
            scroller.ViewChanged += Scroller_ViewChanged;
        }

        private void ScrollerOnUnloaded(object sender, RoutedEventArgs e)
        {
            var scroller = (ScrollViewer)sender;

            scroller.Unloaded -= ScrollerOnUnloaded;
            scroller.ViewChanged -= Scroller_ViewChanged;
        }
        private bool _wasTransformed;
        private void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            //check if we scrolled past final header
            var frac = (sender as ScrollViewer).VerticalOffset / ((UIElement)TracksList.Header).ActualSize.Y;
            var progress = Math.Clamp(frac, 0, 1);
            // HeaderImage.BlurValue = progress * 20;

            var exponential = Math.Pow(progress, 2);
            var opacity = 1 - exponential;
            // HeaderImage.Opacity = opacity;

            //at around 75%, we should start transforming the header into a floating one
            if (progress > .4 && !_wasTransformed)
            {
                _wasTransformed = true;
                BaseTrans.Source = Header;
                BaseTrans.Target = SecondMetadataPanel;
                _ = BaseTrans.StartAsync();
            }
            else if (progress <= .4 && _wasTransformed)
            {
                _wasTransformed = false;
                BaseTrans.Source = SecondMetadataPanel;
                BaseTrans.Target = Header;
                _ = BaseTrans.StartAsync();
            }
        }
    }
}

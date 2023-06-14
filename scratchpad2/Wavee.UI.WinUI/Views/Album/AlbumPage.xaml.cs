using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Album
{
    public sealed partial class AlbumPage : UserControl, INavigable
    {
        public AlbumPage()
        {
            this.InitializeComponent();
        }

        public async void NavigatedTo(object parameter)
        {
            if (parameter is NavigationWithImage img)
            {
                var bmp = new BitmapImage();
                Src.Source = bmp;
                bmp.UriSource = new Uri(img.Image, UriKind.RelativeOrAbsolute);
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
    }
}

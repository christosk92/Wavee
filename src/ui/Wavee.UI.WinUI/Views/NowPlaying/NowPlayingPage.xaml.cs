using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Wavee.UI.Features.Listen;
using Wavee.UI.WinUI.Contracts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.Features.NowPlaying.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.NowPlaying
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NowPlayingPage : Page, INavigeablePage<NowPlayingViewModel>
    {
        public NowPlayingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is NowPlayingViewModel vm)
            {
                DataContext = vm;
            }
        }

        public void UpdateBindings()
        {
            //this.Bindings.Update();
        }

        public NowPlayingViewModel ViewModel => DataContext is NowPlayingViewModel vm ? vm : null;
    }
}

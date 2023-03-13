using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.ForYou.Home;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Wavee.UI.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.ForYou.Home
{
    public sealed partial class LocalHomeView : UserControl
    {
        public LocalHomeView(LocalHomeViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
        }
        public LocalHomeViewModel ViewModel
        {
            get;
        }

        public Visibility HasItems(int count)
        {
            return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        // private void AddTracksDrop(object sender, DragEventArgs e)
        // {
        // }
        //
        // private void AddTracksDragOver(object sender, DragEventArgs e)
        // {
        //   
        // }
        private void LocalHomeView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 900)
                VisualStateManager.GoToState(this, "SmallState", false);
            else
                VisualStateManager.GoToState(this, "DefaultState", false);
        }

        private void SeeAllTracksCommand(object sender, TappedRoutedEventArgs e)
        {
            // this.Frame.Navigate(typeof(SeeAllImportedTracksPage), null, new SlideNavigationTransitionInfo()
            // {
            //     Effect = SlideNavigationTransitionEffect.FromLeft
            // });
            NavigationService.Instance.To<SeeAllImportedTracksViewModel>();
        }
    }
}

using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Spotify.Metadata;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.WinUI.Navigation;
using Windows.Foundation.Metadata;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.WinUI.Components;

namespace Wavee.UI.WinUI.Views.Home
{
    public sealed partial class HomePage : UserControl, INavigable, ICacheablePage
    {
        public HomePage()
        {
            ViewModel = new HomeViewModel();
            this.InitializeComponent();
        }

        public HomeViewModel ViewModel { get; }
        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = ((TokenView)sender).SelectedIndex;
            if (ViewModel is not null)
                await ViewModel.FetchView((HomeViewType)selectedIndex);
        }
        public bool NegateBool(bool b)
        {
            return !b;
        }

        private void OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs e)
        {
            if (e.DataContext is CardItem item)
            {
                e.TemplateKey = item.Id.Type switch
                {
                    AudioItemType.Artist => "artist",
                    _ => "regular"
                };
                //e.TemplateKey = (item.Index % 2 == 0) ? "even" : "odd";
            }
        }

        private UIElement _storeditem;

        public void NavigatedTo(object parameter)
        {
            if (_storeditem != null)
            {
                ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView()
                    .GetAnimation("BackConnectedAnimation");
                if (animation != null)
                {
                    // Setup the "back" configuration if the API is present. 
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                    {
                        animation.Configuration = new DirectConnectedAnimationConfiguration();
                    }

                    animation.TryStart(_storeditem);
                    _storeditem = null;
                    //  await collection.TryStartConnectedAnimationAsync(animation, _storeditem, "connectedElement");
                }
            }
        }

        public void NavigatedFrom(NavigationMode mode)
        {

        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 10;
        }

        public void RemovedFromCache()
        {

        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as CardView)?.Id.Type is AudioItemType.Album)
            {
                _storeditem = (sender as UIElement).FindDescendant<ConstrainedBox>();
            }
        }
    }
}

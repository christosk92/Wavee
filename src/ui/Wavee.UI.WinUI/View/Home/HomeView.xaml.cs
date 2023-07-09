using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Input;
using Wavee.Id;
using Wavee.UI.Common;
using Wavee.UI.WinUI.View.Album;
using Wavee.UI.WinUI.View.Artist;
using Wavee.UI.WinUI.View.Playlist;

namespace Wavee.UI.WinUI.View.Home;

public sealed partial class HomeView : UserControl, ICacheablePage, INavigable
{
    public HomeView()
    {
        ViewModel = new HomeViewModel(ShellViewModel.Instance.User);
        _ = ViewModel.Fetch();
        this.InitializeComponent();
    }
    public HomeViewModel ViewModel { get; }
    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 10;
    }

    public void RemovedFromCache()
    {
        //clear data
    }

    public void NavigatedTo(object parameter)
    {
        if (_stored is not null)
        {
            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
            if (animation != null && _stored != null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                animation.TryStart(_stored);
                _stored = null;
            }
        }
    }

    public void NavigatedFrom(NavigationMode mode)
    {

    }


    private async void FilterClicked(object sender, ItemClickEventArgs e)
    {
        await Task.Delay(10);
        var selectedItem = (sender as TokenView)?.SelectedItem;
        if (selectedItem is null)
        {
            selectedItem = string.Empty;
        }
        if (selectedItem is string filter)
        {
            ViewModel.SelectedFilter = filter;
            await ViewModel.Fetch();
            ViewModel.SelectedFilter = filter;
        }
    }

    private UIElement _stored;
    private void CardView_OnOnNavigated(object sender, EventArgs e)
    {
        if (_stored is not null)
            _stored = sender as UIElement;
    }

    public bool Negate(bool b)
    {
        //TODO:
        return !b;
    }

    private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if ((sender as FrameworkElement).Tag is ICardViewModel c)
        {
            switch (c.Type)
            {
                case AudioItemType.Album:
                    NavigationService.Instance.Navigate(typeof(AlbumView), c.Id);
                    break;
                case AudioItemType.Artist:
                    NavigationService.Instance.Navigate(typeof(ArtistView), c.Id);
                    break;
                case AudioItemType.Playlist:
                    NavigationService.Instance.Navigate(typeof(PlaylistView), c.Id);
                    break;
            }
        }
    }
}
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Nito.AsyncEx;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.WinUI.Contracts;
using Wavee.UI.WinUI.Views.Libraries.Components;

namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class ArtistLibraryPage : Page, INavigeablePage<LibraryArtistsViewModel>
{
    private readonly AsyncLock _lock = new();

    public ArtistLibraryPage()
    {
        this.InitializeComponent();
        var c = new TransitionCollection { };
        var t = new NavigationThemeTransition { };
        var i = new EntranceNavigationTransitionInfo();
        t.DefaultNavigationTransitionInfo = i;
        c.Add(t);
        ArtistFrame.Transitions = c;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibraryArtistsViewModel vm)
        {
            DataContext = vm;
           // vm.PropertyChanged += VmOnPropertyChanged;
            await vm.Initialize();
        }
    }

    // private void VmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    // {
    //     if (e.PropertyName == nameof(LibraryArtistsViewModel.SelectedArtist))
    //     {
    //         if (ViewModel.SelectedArtist != null)
    //         {
    //             ArtistsFrame.Navigate(typeof(SelectedLibraryArtistComponent), ViewModel.SelectedArtist);
    //         }
    //     }
    // }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // if (e.Parameter is LibraryArtistsViewModel vm)
        // {
        //     vm.PropertyChanged -= VmOnPropertyChanged;
        // }
    }

    public void UpdateBindings()
    {
        this.Bindings.Update();
    }

    public LibraryArtistsViewModel ViewModel => DataContext is LibraryArtistsViewModel vm ? vm : null;

    private void ItemsView_OnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        var vm = ViewModel;
        var selected = sender.SelectedItem;
        if (selected is LibraryArtistViewModel v)
        {
            vm.SelectedArtist = v;
        }
    }

    private void SortSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        var vm = ViewModel;
        var selected = sender.SelectedItem;
        if (selected is string v)
        {
            vm.SortField = v;
        }
    }


    private async void SortFieldSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.Initialize();
    }

    private async void NameFilterBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        using (await _lock.LockAsync())
        {
            ViewModel.Query = sender.Text;
            await ViewModel.Initialize();
        }
    }

    private void ArtistsItemsViewLoaded(object sender, RoutedEventArgs e)
    {
        var itemsView = sender as ItemsView;
        var scroller = itemsView?.FindDescendant<ScrollView>();
        scroller.ViewChanged += ScrollerOnViewChanged;
    }

    private async void ScrollerOnViewChanged(ScrollView sender, object args)
    {
        using (await _lock.LockAsync())
        {
            //load more items when the user scrolls to the bottom
            var verticalOffset = sender.VerticalOffset;
            var maxVerticalOffset = sender.ScrollableHeight;
            //with a margin of 200

            if (sender.VerticalOffset >= sender.ScrollableHeight - 200)
            {
                await ViewModel.FetchPage(
                    offset: ViewModel.Artists.Count,
                    cancellationToken: CancellationToken.None
                );
            }
        }
    }
}
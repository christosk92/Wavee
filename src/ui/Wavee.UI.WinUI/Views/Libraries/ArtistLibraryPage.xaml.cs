using System;
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Nito.AsyncEx;
using Wavee.UI.Domain.Artist;
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
            this.Bindings.Update(); ;
            DataContext = vm;
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

    public LibraryArtistsViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is LibraryArtistsViewModel vm ? vm : null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

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
        if (selected is ArtistLibrarySortField v)
        {
            vm.SortField = v;
        }
    }


    private async void SortFieldSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.Initialize(true);
    }

    private async void NameFilterBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        using (await _lock.LockAsync())
        {
            ViewModel.Query = sender.Text;
            await ViewModel.Initialize(true);
        }
    }

    private void ArtistDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var container = sender as FrameworkElement;
        var tag = container?.Tag;
        if (tag is string id)
        {
            Constants.NavigationCommand.Execute(id);
        }
    }
}
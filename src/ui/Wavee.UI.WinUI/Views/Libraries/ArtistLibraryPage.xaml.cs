using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.WinUI.Contracts;
using Wavee.UI.WinUI.Views.Libraries.Components;

namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class ArtistLibraryPage : Page, INavigeablePage<LibraryArtistsViewModel>
{
    public ArtistLibraryPage()
    {
        this.InitializeComponent();
        var c = new TransitionCollection { };
        var t = new NavigationThemeTransition { };
        var i = new EntranceNavigationTransitionInfo();
        t.DefaultNavigationTransitionInfo = i;
        c.Add(t);
        ArtistsFrame.ContentTransitions = c;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibraryArtistsViewModel vm)
        {
            DataContext = vm;
            vm.PropertyChanged += VmOnPropertyChanged;
            await vm.Initialize();
        }
    }

    private void VmOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryArtistsViewModel.SelectedArtist))
        {
            if (ViewModel.SelectedArtist != null)
            {
                ArtistsFrame.Navigate(typeof(SelectedLibraryArtistComponent), ViewModel.SelectedArtist);
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (e.Parameter is LibraryArtistsViewModel vm)
        {
            vm.PropertyChanged -= VmOnPropertyChanged;
        }
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
}
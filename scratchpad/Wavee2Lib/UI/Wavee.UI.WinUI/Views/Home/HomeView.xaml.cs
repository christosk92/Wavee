using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.WinUI.UI;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Ids;
using Wavee.UI.Models.Common;
using Wavee.UI.Models.Home;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Home;

public sealed partial class HomeView : UserControl, ICacheablePage
{
    public HomeView()
    {
        ViewModel = new HomeViewModel();
        this.InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }

    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 10;
    }

    public void RemovedFromCache()
    {
        ViewModel.ClearData();
        this.Bindings.StopTracking();
    }
    private void HomeView_OnLoaded(object sender, RoutedEventArgs e)
    {

    }

    private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var addedItems = e.AddedItems;
        if (addedItems.Count > 0)
        {
            var addedItem = e.AddedItems[0] as TokenItem;
            switch (addedItem.Tag)
            {
                case "all":
                    await ViewModel.FetchAll();
                    GC.Collect(); 
                    break;
                case "songs":
                    await ViewModel.FetchSongsOnly();
                    GC.Collect();
                    break;
                case "podcasts":
                    break;
            }
        }
    }

    public bool NegateBool(bool b)
    {
        return !b;
    }

    private void SpotifyItemTapped(object sender, TappedRoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag;
        if (tag is not AudioId id)
        {
            return;
        }

        //if the originalSource contains ButtonsPanel, we tapped on a button and we don't want to navigate
        if (e.OriginalSource is FrameworkElement originalSource
            && originalSource.FindAscendantOrSelf<FrameworkElement>(x => x.Name is "ButtonsPanel") is { })
        {
            return;
        }

        UICommands.NavigateTo.Execute(id);
    }

    private void OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs e)
    {
        if (e.DataContext is CardViewItem item)
        {
            e.TemplateKey = item.Id.Type switch
            {
                AudioItemType.Artist => "artist",
                _ => "regular"
            };
            //e.TemplateKey = (item.Index % 2 == 0) ? "even" : "odd";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.WinUI.Contracts;
using Wavee.UI.Features.Album.ViewModels;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Domain.Album;
using Wavee.UI.Features.Artist.ViewModels;
using System.Text;

namespace Wavee.UI.WinUI.Views.Album;

public sealed partial class AlbumPage : Page, INavigeablePage<AlbumViewViewModel>
{
    public AlbumPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is AlbumViewViewModel vm)
        {
            DataContext = vm;
            await vm.Initialize();
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public AlbumViewViewModel ViewModel => DataContext is AlbumViewViewModel vm ? vm : null;

    public object CreateCvs(IReadOnlyCollection<AlbumDiscViewModel> readOnlyCollection)
    {
        if (readOnlyCollection is null)
        {
            return null;
        }

        if (readOnlyCollection.Count is 1)
        {
            return readOnlyCollection.First().Tracks;
        }


        var cvs = new CollectionViewSource
        {
            Source = readOnlyCollection,
            IsSourceGrouped = true,
        };
        return cvs.View;
    }
}
using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Feed;
using Wavee.UI.WinUI.Views.Library.Components;
using Wavee.UI.WinUI.Views.NowPlaying;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryRootView : UserControl
{
    public LibraryRootView(LibraryRootViewModel libraryRootViewModel)
    {
        this.InitializeComponent();

        ViewModel = libraryRootViewModel;
        var vmToView = new Dictionary<Type, (Type, CachingPolicy)>
        {
            [typeof(LibraryTracksViewModel)] = (typeof(LibraryTracksView), CachingPolicy.AlwaysYesPolicy),
            [typeof(LibraryArtistsViewModel)] = (typeof(LibraryArtistsView), CachingPolicy.AlwaysYesPolicy),
            [typeof(LibraryAlbumsViewModel)] = (typeof(LibraryAlbumsView), CachingPolicy.AlwaysYesPolicy),
        };
        ViewModel.SetNavigationController(new ContentControlNavigationController(this.MainContent, vmToView));
    }
    public LibraryRootViewModel ViewModel { get; }

    public object ComponentToCommands(ILibraryComponentViewModel? libraryComponentViewModel)
    {
        if (libraryComponentViewModel is null) return null;

        return new LibraryFilteringComponentBar(libraryComponentViewModel);
    }
}
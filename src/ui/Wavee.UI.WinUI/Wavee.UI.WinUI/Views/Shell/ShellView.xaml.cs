using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Feed;
using Wavee.UI.WinUI.Views.Library;
using Wavee.UI.WinUI.Views.NowPlaying;
using static Wavee.UI.WinUI.Navigation.ContentControlNavigationController;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class ShellView : UserControl
{
    public ShellView(ShellViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;

        var vmToView = new Dictionary<Type, (Type, CachingPolicy)>
        {
            [typeof(FeedViewModel)] = (typeof(FeedView), CachingPolicy.AlwaysYesPolicy),
            [typeof(LibraryRootViewModel)] = (typeof(LibraryRootView), CachingPolicy.AlwaysYesPolicy),
            [typeof(NowPlayingViewModel)] = (typeof(NowPlayingView), CachingPolicy.AlwaysYesPolicy),
        };
        viewModel.SetNavigationController(new ContentControlNavigationController(this.MainContent, vmToView));
    }

    public ShellViewModel ViewModel { get; }
}
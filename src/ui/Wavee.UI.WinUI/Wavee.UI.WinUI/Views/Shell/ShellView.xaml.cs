using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.Media;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Providers;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Feed;
using Wavee.UI.WinUI.Views.Library;
using Wavee.UI.WinUI.Views.NowPlaying;
using static System.Runtime.InteropServices.JavaScript.JSType;
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

    public GridLength RightSidebarOpenToDefaultWidth(bool b)
    {
        return b ? new GridLength(200) : new GridLength(0);
    }

    public double RightSidebarOpenToMinWidth(bool b, double s)
    {
        return b ? s : 0;
    }
}
using System;
using System.Linq;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.ViewModels.ForYou;
using Wavee.UI.ViewModels.Shell;
using WinUIEx;
using Wavee.UI.Navigation;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class ShellView : UserControl
{

    public ShellView(ShellViewModel viewmodel)
    {
        ViewModel = viewmodel;
        this.InitializeComponent();
    }
    public ShellViewModel ViewModel { get; }
    public NavigationService NavigationService { get; } = NavigationService.Instance;

    public bool ShouldShowHeader(SidebarItemViewModel o)
    {
        return
            o is RecommendedViewModelFactory
            {
                ForService: not ServiceType.Local
            };
    }

    public Visibility ShouldShowHeaderVisibility(SidebarItemViewModel o)
    {
        return ShouldShowHeader(o) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ViewPanel_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        //var getIndex
        if (args.InvokedItemContainer.Tag is string tagId)
        {
            ViewModel.SelectedSidebarItem = ViewModel.SidebarItems.Single(a => a.Id == tagId);
        }
    }

    // private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    // {
    //     var picker = new FileOpenPicker();
    //     WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MWindow.GetWindowHandle());
    //     picker.FileTypeFilter.Add(".mp3");
    //     picker.FileTypeFilter.Add(".ogg");
    //     var openfile = await picker.PickSingleFileAsync();
    //     if (openfile != null)
    //     {
    //         var path = openfile.Path;
    //         var player = Ioc.Default.GetRequiredService<LocalFilePlayer>();
    //         player.PlayFile(path);
    //     }
    // }
}
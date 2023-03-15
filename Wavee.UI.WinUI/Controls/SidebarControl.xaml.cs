using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.ViewModels.ForYou.Recommended;
using CommunityToolkit.WinUI.UI;
using Wavee.UI.WinUI.Views.Identity;

namespace Wavee.UI.WinUI.Controls;
public sealed partial class SidebarControl : NavigationView
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ShellViewModel), typeof(SidebarControl), new PropertyMetadata(default(ShellViewModel)));

    public SidebarControl()
    {
        this.InitializeComponent();
    }
    public ShellViewModel ViewModel
    {
        get => (ShellViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

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
            ViewModel.SelectedSidebarItem.NavigateTo();
        }
    }

    private bool _wasExpandedImage;
    private void ViewPanel_OnPaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
    {
        this.FindDescendant<UserProfileCard>()!.Visibility = Visibility.Collapsed;
        _wasExpandedImage = ViewModel.UserViewModel.User.UserData.SidebarExpanded;
        if (_wasExpandedImage)
        {
            ViewModel.UserViewModel.User.UserData.SidebarExpanded = false;
        }
    }

    private void ViewPanel_OnPaneOpening(NavigationView sender, object args)
    {
        this.FindDescendant<UserProfileCard>()!.Visibility = Visibility.Visible;
        ViewModel.UserViewModel.User.UserData.SidebarExpanded = _wasExpandedImage;
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Shell;
using Wavee.UI.Shell.Sidebar;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Shell.Sidebar;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class ShellPage : UserControl
{
    public ShellPage()
    {
        ViewModel = new ShellViewModel();
        this.InitializeComponent();
        NavigationService = new NavigationService(NavigationContentFrame);
    }

    public static NavigationService NavigationService { get; private set; }

    public ShellViewModel ViewModel { get; }

    private void SidebarListViewCOntentContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is HeaderSidebarItem)
        {
            if (args.ItemContainer is { } listViewItem)
            {
                listViewItem.IsHitTestVisible = false;
            }
        }
    }

    private void SplitView_OnPaneOpening(SplitView sender, object args)
    {
        if (SidebarListView is null) return;
        foreach (var item in SidebarListView.Items)
        {
            var container = SidebarListView.ContainerFromItem(item);
            if (container is ListViewItem { ContentTemplateRoot: CountedSidebarView countedV })
            {
                countedV.IsOpen = true;
            }
        }
    }

    private void SplitView_OnPaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
    {
        foreach (var item in SidebarListView.Items)
        {
            var container = SidebarListView.ContainerFromItem(item);
            if (container is CountedSidebarView countedV)
            {
                countedV.IsOpen = false;
            }
        }
    }

    private void SidebarListView_OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {
        if (args.Item is HeaderSidebarItem)
        {
            if (args.ItemContainer is { } listViewItem)
            {
                listViewItem.IsHitTestVisible = false;
            }
        }
    }

    private void SidebarListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //navigate
        NavigationService.Navigate(typeof(HomeView));
    }
}
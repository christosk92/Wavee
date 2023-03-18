using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Navigation;
using Wavee.UI.ViewModels.Shell.Sidebar;
using Wavee.UI.WinUI.Helpers.AttachedProperties;
using Wavee.UI.WinUI.Interfaces.Services;

namespace Wavee.UI.WinUI.Services;

public class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;

    private readonly IPageService _pageService;

    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public NavigationViewService(INavigationService navigationService, IPageService pageService)
    {
        _navigationService = navigationService;
        _pageService = pageService;
    }

    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
    }

    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public ISidebarItem? GetSelectedItem(Type pageType, object? parameter)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItemsSource as IEnumerable<object>, pageType, parameter) ??
                   GetSelectedItem(_navigationView.FooterMenuItems, pageType, parameter);
        }

        return null;
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) =>
        _navigationService.GoBack();

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var selectedItem = args.InvokedItemContainer as NavigationViewItem;

        if (selectedItem.DataContext is ISidebarItem sidebarItem)
        {
            _navigationService.NavigateTo(sidebarItem.NavigateTo?.FullName,
                sidebarItem is CountedSidebarItem s ? s.NavigateToParameter : null);
        }
    }

    private ISidebarItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType, object? parameter)
    {
        foreach (var item in menuItems.OfType<ISidebarItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                if (item is CountedSidebarItem param)
                {
                    if (param.NavigateToParameter is LibraryNavigationParameters l &&
                        parameter is LibraryNavigationParameters k)
                    {
                        if (l.NavigateTo == k.NavigateTo)
                            return item;
                    }
                    else
                    {
                        if (param.NavigateToParameter?.Equals(parameter) is true)
                        {
                            return item;
                        }
                    }
                }
                else
                {
                    return item;
                }
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(ISidebarItem sidebarItem, Type sourcePageType)
    {
        return _pageService.GetPageType(sidebarItem.NavigateTo?.FullName) == sourcePageType
            || sidebarItem.NavigateTo?.FullName == sourcePageType.FullName;
        return false;
    }
}
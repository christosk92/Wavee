using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Shell.Sidebar;

namespace Wavee.UI.WinUI.Interfaces.Services;

public interface INavigationViewService
{
    IList<object>? MenuItems
    {
        get;
    }

    object? SettingsItem
    {
        get;
    }


    void Initialize(NavigationView navigationView);

    void UnregisterEvents();

    ISidebarItem? GetSelectedItem(Type pageType, object? parameter);
}
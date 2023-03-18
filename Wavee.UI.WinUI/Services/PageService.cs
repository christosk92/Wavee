using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.ViewModels.Home;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.WinUI.Views;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Library;

namespace Wavee.UI.WinUI.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
        Configure<LocalHomeViewModel, LocalHomeView>();
        Configure<LibraryRootViewModel, LibraryRootView>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (!_pages.TryGetValue(key, out pageType))
                {
                    throw new ArgumentException(
                        $"Page not found: {key}. Did you forget to call PageService.Configure?");
                }
            }
            else
            {
                return typeof(NoView);
            }
        }

        return pageType;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.Any(p => p.Value == type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}
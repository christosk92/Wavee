using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Services;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI.Navigation;

public class ContentControlNavigationController : INavigationController
{
    private ContentPresenter _mainContent;
    private readonly Dictionary<Type, (Type, CachingPolicy)> _vmToView = new();
    private readonly Stack<CachedPageRecord> _backstack = new();

    public ContentControlNavigationController(ContentPresenter mainContent,
        Dictionary<Type, (Type, CachingPolicy)> viewMapping)
    {
        _mainContent = mainContent;
        _vmToView = viewMapping;
    }

    public void NavigateTo(object viewmodel)
    {
        var type = viewmodel.GetType();
        var (viewType, cachingPolicy) = _vmToView[type];
        if (!TryGetFromCache(viewType, out var view))
        {
            view = Activator.CreateInstance(viewType, viewmodel);
        }

        _backstack.Push(new CachedPageRecord(viewType, view, DateTimeOffset.UtcNow, cachingPolicy));
        _mainContent.Content = view;
    }

    private bool TryGetFromCache(Type type, out object? view)
    {
        int depth = 0;
        foreach (var record in _backstack)
        {
            if (record.Type == type && record.Value is not null)
            {
                if (record.Policy.ShouldKeepInCache(record, depth))
                {
                    view = record.Value;
                }
                else
                {
                    record.Value = null;
                    view = null;
                }

                return true;
            }

            depth++;
        }

        view = default;
        return false;
    }

    public void Dispose()
    {
        foreach (var record in _backstack)
        {
            record.Value = null;
        }

        _backstack.Clear();
        _mainContent = null;
    }
}
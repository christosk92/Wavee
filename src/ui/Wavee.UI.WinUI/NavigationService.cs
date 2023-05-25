using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI;

public sealed class NavigationService
{
    private ContentControl _contentControl;

    private object? _lastParameter;
    private readonly Stack<(Type Type, object? Parameter)> _backStack = new();

    private readonly Dictionary<Type, (INavigablePage Page, object? WithParameter, int InsertedAt)>
        _cachedPages = new();

    public NavigationService(ContentControl frame)
    {
        _contentControl = frame;
        Instance = this;
    }

    public static NavigationService Instance { get; private set; } = null!;

    public void Navigate(Type pageType,
        object? parameter = null,
        bool addToStack = true)
    {
        if (!typeof(INavigablePage).IsAssignableFrom(pageType))
        {
            throw new ArgumentException("Page type must implement INavigablePage.", nameof(pageType));
        }

        //couple of considerations:
        //1) Pages can be cached
        //2) Pages can also refused to be cached
        //3) Pages can be cached with a parameter
        //4) In teh case of a cached page, we need to check if the parameter is the same
        //5) If the parameter is the same, we don't want to navigate to the page again
        //6) If the parameter is different, we want to navigate to the page again (new instance) and remove the cached page and add this new one to the cache

        //check the current page
        if (_contentControl.Content is INavigablePage currentPage)
        {
            var currentPageType = currentPage.GetType();
            if (currentPageType == pageType && _lastParameter == parameter)
            {
                return;
            }

            if (addToStack)
                _backStack.Push((currentPageType, parameter));

            //clear caches
            if (_cachedPages.TryGetValue(pageType, out var cachedPage))
            {
                //if the current page is cached, check if the parameter is the same
                if (!EqualityComparer<object>.Default.Equals(parameter, cachedPage.WithParameter))
                {
                    //if the parameter is different, we want to navigate to the page again (new instance) and remove the cached page and add this new one to the cache
                    if (_cachedPages.TryGetValue(pageType, out var potentialCachedPage))
                    {
                        _cachedPages.Remove(pageType);
                        potentialCachedPage.Page.RemovedFromCache();
                    }
                }
                else
                {
                    //check if the page should be kept in cache
                    if (!currentPage.ShouldKeepInCache(_backStack.Count - cachedPage.InsertedAt))
                    {
                        //if the page should not be kept in cache, remove it from the cache
                        currentPage.RemovedFromCache();
                        _cachedPages.Remove(currentPageType);
                    }
                }
            }
            else
            {
                if (!currentPage.ShouldKeepInCache(_backStack.Count))
                {
                    _cachedPages.Remove(currentPageType);
                    currentPage.RemovedFromCache();
                }
            }

            currentPage.ViewModel.IfSome(x => x.OnNavigatedFrom());
        }

        _lastParameter = parameter;
        //now that our cache is up to date, we can check if the page is cached
        if (_cachedPages.TryGetValue(pageType, out var cachedPage2))
        {
            //if the page is cached, we want to navigate to the cached page
            _contentControl.Content = cachedPage2.Page;
        }
        else
        {
            //if the page is not cached, we want to create a new instance of the page
            var nextPage = (INavigablePage)Activator.CreateInstance(pageType);
            nextPage.ViewModel.IfSome(x => x.OnNavigatedTo(parameter));
            _contentControl.Content = nextPage;
            _cachedPages.Add(pageType, (nextPage, parameter, _backStack.Count));
        }

        Navigated?.Invoke(this, pageType);
        // if (_contentControl.Content is INavigablePage currentPage)
        // {
        //     var currentPageType = currentPage.GetType();
        //     if (currentPageType == pageType && _lastParameter == parameter)
        //     {
        //         return;
        //     }
        //
        //     if (addToStack)
        //         _backStack.Push((currentPageType, parameter));
        //
        //     if (_cachedPages.TryGetValue(currentPageType,
        //             out var cached)
        //         && cached.WithParameter == parameter)
        //     {
        //         if (!cached.Page.ShouldKeepInCache(_backStack.Count - cached.InsertedAt))
        //         {
        //             _cachedPages.Remove(cached.Page.GetType());
        //         }
        //     }
        //     else
        //     {
        //         if (currentPageType == pageType && _lastParameter != parameter)
        //         {
        //             _cachedPages.Remove(currentPageType);
        //         }
        //         else
        //         {
        //             if (currentPage.ShouldKeepInCache(0))
        //             {
        //                 _cachedPages[currentPageType] = (currentPage, parameter, _backStack.Count);
        //             }
        //         }
        //     }
        //
        //     currentPage.ViewModel.IfSome(x => x.OnNavigatedFrom());
        // }
        //
        // _lastParameter = parameter;
        // INavigablePage nextPage;
        // if (_cachedPages.TryGetValue(pageType, out var cachedPage))
        // {
        //     nextPage = cachedPage.Page;
        // }
        // else
        // {
        //     nextPage = (INavigablePage)Activator.CreateInstance(pageType);
        //     //nextPage = (INavigablePage)Ioc.Default.GetService(pageType);
        //     nextPage.ViewModel.IfSome(x => x.OnNavigatedTo(parameter));
        // }
        //
        // _contentControl.Content = nextPage;
    }


    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => false;
    public event EventHandler<Type>? Navigated;

    public void GoBack()
    {
        if (CanGoBack)
        {
            (Type Type, object? Parameter) previousPageType = _backStack.Pop();
            Navigate(previousPageType.Type, parameter: previousPageType.Parameter, false);
        }
    }

    public void Clear()
    {
        _cachedPages.Clear();
        _backStack.Clear();
        _contentControl = null;
        _lastParameter = null;
    }
}
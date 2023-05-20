using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI;

public sealed class NavigationService
{
    private ContentControl _contentControl;

    private object? _lastParameter;
    private readonly Stack<(Type Type, object? Parameter)> _backStack = new();
    private readonly Dictionary<Type, (INavigablePage Page, object? WithParameter, int InsertedAt)> _cachedPages = new();

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

        if (_contentControl.Content is INavigablePage currentPage)
        {
            if (currentPage.GetType() == pageType && _lastParameter == parameter)
            {
                return;
            }

            if (addToStack)
                _backStack.Push((currentPage.GetType(), parameter));

            if (_cachedPages.TryGetValue(currentPage.GetType(), out var cached))
            {
                if (!cached.Page.ShouldKeepInCache(_backStack.Count - cached.InsertedAt))
                {
                    _cachedPages.Remove(cached.Page.GetType());
                }
            }
            else
            {
                if (currentPage.ShouldKeepInCache(0))
                {
                    _cachedPages[currentPage.GetType()] = (currentPage, parameter, _backStack.Count);
                }
            }

            currentPage.ViewModel.IfSome(x => x.OnNavigatedFrom());
        }

        _lastParameter = parameter;
        INavigablePage nextPage;
        if (_cachedPages.TryGetValue(pageType, out var cachedPage))
        {
            nextPage = cachedPage.Page;
        }
        else
        {
            nextPage = (INavigablePage)Activator.CreateInstance(pageType);
            //nextPage = (INavigablePage)Ioc.Default.GetService(pageType);
            nextPage.ViewModel.IfSome(x => x.OnNavigatedTo(parameter));
        }

        _contentControl.Content = nextPage;
    }


    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => false;

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
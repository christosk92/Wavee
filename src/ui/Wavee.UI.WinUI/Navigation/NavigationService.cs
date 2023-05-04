using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Navigation;

public sealed class NavigationService : ObservableObject
{
    private ContentControl _contentControl;

    private object? _lastParameter;
    private readonly Stack<(Type Type, object? Parameter)> _backStack = new();

    private readonly Dictionary<Type, (INavigablePage Page, object? WithParameter, int InsertedAt)>
        _cachedPages = new();

    public NavigationService(ContentControl frame)
    {
        _contentControl = frame;
    }

    public void Navigate(Type pageType, object? parameter = null, bool addToStack = true)
    {
        if (!typeof(INavigablePage).IsAssignableFrom(pageType))
        {
            throw new ArgumentException("Page type must implement INavigablePage.", nameof(pageType));
        }

        //check if current page is the same as the new page with the same parameter
        if (_lastParameter?.Equals(parameter) is true && _contentControl.Content?.GetType() == pageType)
        {
            return;
        }

        if (addToStack)
        {
            _backStack.Push((_contentControl.Content?.GetType() ?? typeof(object), _lastParameter));
        }


        //if page is cached, use it (provided the parameter is the same)
        if (_cachedPages.TryGetValue(pageType, out var cachedPage) &&
            ((cachedPage.WithParameter is null && parameter is null) ||
            cachedPage.WithParameter?.Equals(parameter) is true))
        {
            //check if we should keep the page in cache
            var insertedAt = cachedPage.InsertedAt;
            var depth = _backStack.Count - insertedAt - 1;

            if (cachedPage.Page.ShouldKeepInCache(depth))
            {
                _contentControl.Content = cachedPage.Page;
                _lastParameter = parameter;
                Navigated?.Invoke(this, (pageType, parameter));
                OnPropertyChanged(nameof(CanGoBack));
                //bump the page to the top of the cache
                _cachedPages.Remove(pageType);
                _cachedPages.Add(pageType, (cachedPage.Page, parameter, _backStack.Count));
                return;
            }
        }

        //if the page is cached but with a different parameter, remove it from cache
        if (_cachedPages.TryGetValue(pageType, out cachedPage))
        {
            _cachedPages.Remove(pageType);
        }

        if (_contentControl.Content is INavigablePage oldPage)
        {
            oldPage.ViewModel.OnNavigatedFrom();
        }

        //create new page
        var nextPage = Ioc.Default.GetService(pageType);
        if (nextPage is not INavigablePage navigablePage)
        {
            throw new ArgumentException("Page type must implement INavigablePage.", nameof(pageType));
        }

        //cache the page
        _cachedPages.Add(pageType, (navigablePage, parameter, _backStack.Count));
        navigablePage.ViewModel.OnNavigatedTo(parameter);

        //set the page
        _contentControl.Content = navigablePage;
        _lastParameter = parameter;
        Navigated?.Invoke(this, (pageType, parameter));
        OnPropertyChanged(nameof(CanGoBack));
    }


    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => false;
    public event EventHandler<(Type Page, object? Parameter)> Navigated;

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
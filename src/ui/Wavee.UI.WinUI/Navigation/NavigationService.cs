using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.WinUI.View.Album;
using Wavee.UI.WinUI.View.Artist;
using Wavee.UI.WinUI.View.Home;

namespace Wavee.UI.WinUI.Navigation;
public record CachedPage(WeakReference PageReference, Type Type, object? WithParameter, int InsertedAt);
public record GeneralBackStackItem(Type Type, object? Parameter);
public sealed class NavigationService : ObservableObject
{
    private ContentPresenter _contentControl;
    private object? _lastPrameter;

    private readonly Stack<GeneralBackStackItem> _backStack = new();
    private readonly HashSet<CachedPage> _cachedPages = new();

    public NavigationService(ContentPresenter frame)
    {
        _contentControl = frame;
        Instance = this;
    }

    public static NavigationService Instance { get; private set; } = null!;

    private static readonly Dictionary<Type, Delegate> _constructors = new();

    public void Navigate(Type pageType,
        object? parameter = null,
        bool addToStack = true,
        bool goingBackForSure = false)
    {
        EvaluateCache();

        if (_contentControl.Content is INavigable x)
        {
            //check if we are navigating BACK
            var goingBackByBacktack = addToStack && _backStack.Any() && (_backStack.Peek().Type == pageType && Equals(_backStack.Peek().Parameter, parameter));
            //also make sure the page is cached, if so we are going back
            var goingBackByCache = _cachedPages.Any(cp => cp.PageReference.IsAlive && cp.Type == pageType && Equals(cp.WithParameter, parameter));
            goingBackByBacktack = goingBackByBacktack && goingBackByCache;
            var goingBack = goingBackForSure || goingBackByBacktack;
            x.NavigatedFrom(goingBack ? NavigationMode.Back : NavigationMode.New);
        }
        if (addToStack && _contentControl.Content != null)
        {
            _backStack.Push(new GeneralBackStackItem(_contentControl.Content.GetType(), _lastPrameter));
        }


        var cachedPage = _cachedPages.FirstOrDefault(cp => cp.PageReference.IsAlive && cp.Type == pageType && Equals(cp.WithParameter, parameter));

        if (cachedPage != null)
        {
            // Use the page from the cache
            _contentControl.Content = null;
            GC.Collect();
            if (cachedPage.PageReference.Target is INavigable navigablePage)
                navigablePage.NavigatedTo(parameter);

            _contentControl.Content = cachedPage.PageReference.Target;

            // Remove the old cache entry
            _cachedPages.Remove(cachedPage);
            _cachedPages.Add(new CachedPage(new WeakReference(cachedPage.PageReference.Target), pageType, parameter, _backStack.Count));
        }
        else
        {
            _contentControl.Content = null;
            GC.Collect();
            var constructor = _constructors.GetValueOrDefault(pageType);

            var page = constructor.DynamicInvoke();
            if (page is INavigable navigablePage)
                navigablePage.NavigatedTo(parameter);
            _contentControl.Content = page;

            if (page is ICacheablePage cacheablePage)
            {
                _cachedPages.Add(new CachedPage(new WeakReference(cacheablePage), pageType, parameter, _backStack.Count));
            }
        }
        _lastPrameter = parameter;
        Navigated?.Invoke(this, (pageType, parameter));
        this.OnPropertyChanged(nameof(CanGoBack));
    }
    private void EvaluateCache()
    {
        _cachedPages.RemoveWhere(cp =>
        {
            if (cp.PageReference.IsAlive && cp.PageReference.Target is ICacheablePage cacheablePage)
            {
                if (!cacheablePage.ShouldKeepInCache(_backStack.Count - cp.InsertedAt))
                {
                    cacheablePage.RemovedFromCache();
                    cp.PageReference.Target = null;
                    return true;
                }
            }
            else
            {
                // Page was collected by GC
                return true;
            }
            return false;
        });
    }

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => false;
    public event EventHandler<(Type Tp, object Prm)>? Navigated;

    public void GoBack()
    {
        if (CanGoBack)
        {
            var backItem = _backStack.Pop();
            Navigate(backItem.Type, backItem.Parameter, addToStack: false, true);
        }
    }

    public void Clear()
    {
        _cachedPages.Clear();
        _backStack.Clear();
        _contentControl = null;
        _lastPrameter = null;
    }

    static NavigationService()
    {
        var types = new[]
        {
            typeof(HomeView),
            typeof(AlbumView),
            typeof(ArtistView)
        };

        static void RegisterConstructor(Type type)
        {
            // // Make a NewExpression that calls the ctor with the args we just created
            // var ctor = pageType.GetConstructor(Type.EmptyTypes);
            // var argsExp = Expression.NewArrayInit(typeof(object), Expression.Constant(parameter));
            // NewExpression newExp = Expression.New(ctor, argsExp);

            var emptyConstructorExp = Expression.New(type);

            // Create a lambda with the New expression as body and our param object[] as arg
            // LambdaExpression lambda = Expression.Lambda(pageType, newExp, param);
            var lambda = Expression.Lambda(emptyConstructorExp);


            // Compile it
            var func = lambda.Compile();
            _constructors.Add(type, func);
        }

        foreach (var type in types)
        {
            RegisterConstructor(type);
        }
    }
}
public record NavigateToObject(Type To, object? Parameter = null);

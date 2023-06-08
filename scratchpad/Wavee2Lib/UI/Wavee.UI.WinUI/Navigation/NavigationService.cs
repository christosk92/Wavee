using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.WinUI.Views.Artist;
using Wavee.UI.WinUI.Views.Browse;
using Wavee.UI.WinUI.Views.Home;

namespace Wavee.UI.WinUI.Navigation;
public record CachedPage(object Page, object? WithParameter, int InsertedAt);

public sealed class NavigationService
{
    private ContentPresenter _contentControl;

    private object? _lastParameter;
    private readonly Stack<(Type Type, object? Parameter)> _backStack = new();


    private readonly System.Collections.Generic.HashSet<CachedPage> _cachedPages = new();

    // private readonly Dictionary<Type, (INavigablePage Page, object? WithParameter, int InsertedAt)>
    //     _cachedPages = new();
    static NavigationService()
    {
        var types = new[]
        {
            typeof(HomeView),
            typeof(BrowseView),
            typeof(ArtistRootView)
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
    public NavigationService(ContentPresenter frame)
    {
        _contentControl = frame;
        Instance = this;
    }

    public static NavigationService Instance { get; private set; } = null!;

    private static Dictionary<Type, Delegate> _constructors = new();

    public void Navigate(Type pageType,
        object? parameter = null,
        bool addToStack = true)
    {
        //every navigation: the cache should be re-evaluated
        //if a cached page is accessed again, it should be put at the top again

        if (_contentControl.Content is { } currentPage)
        {
            _backStack.Push((currentPage.GetType(), null));
            //            currentPage.ViewModel.IfSome(x => x.OnNavigatedFrom());
        }

        //re-evaluate the cache
        foreach (var cachedPage in _cachedPages.ToArray())
        {
            var currentDepth = _backStack.Count - cachedPage.InsertedAt - 1;
            if (cachedPage.Page is ICacheablePage cacheablePage)
            {
                if (!cacheablePage.ShouldKeepInCache(currentDepth))
                {
                    _cachedPages.Remove(cachedPage);
                    cacheablePage.RemovedFromCache();
                }
            }
        }



        //1) Check if cached page exists
        if (_cachedPages.SingleOrDefault(x =>
                x.Page.GetType() == pageType
                && (x.WithParameter is null || (
                    x.WithParameter is not null
                    && parameter is not null
                    && x.WithParameter.Equals(parameter)))) is { } potentialCache)
        {
            Debug.WriteLine("Setting from cache");
            //if the id is the same, just navigate to it
            _contentControl.Content = potentialCache.Page;
            Debug.WriteLine("Set from cache");
            _lastParameter = parameter;
            //remove the old entry and add it again
            _cachedPages.Remove(potentialCache);
            _cachedPages.Add(potentialCache with
            {
                InsertedAt = _backStack.Count
            });
        }
        else
        {
            //if not, create a new one
            var func = _constructors[pageType];
            var newPage = func.DynamicInvoke();
            var newEntry = new CachedPage(newPage, parameter, _backStack.Count);
            _cachedPages.Add(newEntry);
            if (newPage is INavigateablePage navigablePage)
            {
                navigablePage.NavigatedTo(parameter);
            }
            // newPage.ViewModel.IfSome(x => x.OnNavigatedTo(parameter));
            // newPage.NavigatedTo(parameter);

            _contentControl.Content = newPage;
            _lastParameter = parameter;
        }
        Navigated?.Invoke(this, (pageType, parameter));
    }


    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => false;
    public event EventHandler<(Type Tp, object Prm)>? Navigated;

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

public interface INavigateablePage
{
    void NavigatedTo(object? parameter);
}

public interface ICacheablePage
{
    bool ShouldKeepInCache(int currentDepth);
    void RemovedFromCache();
}

// public sealed class NavigationService
// {
//     private Frame _contentControl;
//
//     // private object? _lastParameter;
//     // private readonly Stack<(Type Type, object? Parameter)> _backStack = new();
//
//     // private readonly HashSet<CachedPage> _cachedPages = new();
//
//     // private readonly Dictionary<Type, (INavigablePage Page, object? WithParameter, int InsertedAt)>
//     //     _cachedPages = new();
//
//     public NavigationService(Frame frame)
//     {
//         _contentControl = frame;
//         Instance = this;
//         frame.Navigated += FrameOnNavigated;
//     }
//
//     private void FrameOnNavigated(object sender, NavigationEventArgs e)
//     {
//         Navigated?.Invoke(this, (e.SourcePageType, e.Parameter));
//     }
//
//     public static NavigationService Instance { get; private set; } = null!;
//
//     public void Navigate(Type pageType,
//         object? parameter = null,
//         bool addToStack = true)
//     {
//         if (!typeof(INavigablePage).IsAssignableFrom(pageType))
//         {
//             throw new ArgumentException("Page type must implement INavigablePage.", nameof(pageType));
//         }
//
//         //every navigation: the cache should be re-evaluated
//         //if a cached page is accessed again, it should be put at the top again
//
//
//
//         Navigated?.Invoke(this, (pageType, parameter));
//     }
//
//
//     public bool CanGoBack => _contentControl.CanGoBack;
//     public bool CanGoForward => false;
//     public event EventHandler<(Type Tp, object Prm)>? Navigated;
//
//     public void GoBack()
//     {
//
//     }
//
//     public void Clear()
//     {
//         _contentControl = null;
//     }
// }
//

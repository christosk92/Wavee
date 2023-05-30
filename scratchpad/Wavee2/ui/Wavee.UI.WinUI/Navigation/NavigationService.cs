using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Navigation;

public static class NavigationService
{
    public static ContentPresenter ContentPresenter { get; set; }

    private static Stack<Type> _history = new Stack<Type>();

    private static readonly System.Collections.Generic.HashSet<CachedItem> _cache = new();

    private static object? _lastParameter;
    public static void Navigate(
        Type type,
        object? parameter = null)
    {
        //check for cache
        //re-evaulate the cache
        if (ContentPresenter.Content?.GetType() == type &&
            EqualityComparer<object>.Default.Equals(_lastParameter, parameter))
        {
            return;
        }
        _lastParameter = parameter;
        foreach (var potentialRemoval in _cache.Where(x => x.InsertedAt < _cache.Count - 1))
        {
            //set page to null
            if (potentialRemoval.Page is ICacheablePage cp)
            {
                cp.RemovedFromCache();
            }

            potentialRemoval.Page = null;
        }

        _history.Push(type);
        if (_cache.SingleOrDefault(x => x.Type == type
                                        && EqualityComparer<object>.Default.Equals(x.Parameter, parameter)) is
            { } cachedItem)
        {
            if (cachedItem.Page is not null)
            {
                ContentPresenter.Content = cachedItem.Page;
                if (cachedItem.Page is INavigationAwareView v)
                {
                    v.OnNavigatedTo(parameter);
                }

                //update inserted at
                _cache.Remove(cachedItem);
                _cache.Add(new CachedItem(type, (ICacheablePage)cachedItem.Page, parameter, _cache.Count));
                return;
            }
            else
            {
                _cache.Remove(cachedItem);
            }
        }

        var page = (UserControl)Activator.CreateInstance(type)!;
        if (page is INavigationAwareView view)
        {
            view.OnNavigatedTo(parameter);
        }

        ContentPresenter.Content = page;
        if (page is ICacheablePage cacheablePage)
        {
            if (cacheablePage.ShouldCache(0))
            {
                _cache.Add(new CachedItem(type, cacheablePage, parameter, _cache.Count));
            }
        }
    }

}

internal class CachedItem
{
    public CachedItem(Type Type,
        ICacheablePage Page,
        object? Parameter, int InsertedAt)
    {
        this.Type = Type;
        this.Page = Page;
        this.Parameter = Parameter;
        this.InsertedAt = InsertedAt;
    }

    public Type Type { get; init; }
    public object Page { get; set; }
    public object Parameter { get; init; }
    public int InsertedAt { get; init; }

    public void Deconstruct(out Type Type, out object Page, out object? Parameter, out int InsertedAt)
    {
        Type = this.Type;
        Page = this.Page;
        Parameter = this.Parameter;
        InsertedAt = this.InsertedAt;
    }
}
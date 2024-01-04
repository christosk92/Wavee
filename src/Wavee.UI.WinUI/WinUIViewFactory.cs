using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Tango.Types;
using Wavee.UI.Navigation;

namespace Wavee.UI.WinUI;

internal sealed class WinUIViewFactory : IViewFactory
{
    private readonly IReadOnlyDictionary<Type, (Type, ViewType)> _viewModelToViewMap;

    internal WinUIViewFactory(IReadOnlyDictionary<Type, (Type, ViewType)> viewModelToViewMap)
    {
        _viewModelToViewMap = viewModelToViewMap;
    }

    public Option<(Type, ViewType)> ViewType<TViewModel>()
    {
        if (_viewModelToViewMap.TryGetValue(typeof(TViewModel), out var viewType))
        {
            return viewType;
        }

        return Option<(Type, ViewType)>.None();
    }

    public static WinUIViewFactoryBuilder Create() => new();
}

internal sealed class WinUIViewFactoryBuilder
{
    private Dictionary<Type, (Type, ViewType)> _viewModelToViewMap = new();

    public WinUIViewFactoryBuilder Add<TViewModel, TView>(ViewType type)
    {
        _viewModelToViewMap.Add(typeof(TViewModel), (typeof(TView), type));
        return this;
    }

    public WinUIViewFactory Build() => new(_viewModelToViewMap);
}
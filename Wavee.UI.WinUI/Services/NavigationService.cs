using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Interfaces;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.WinUI.Helpers.Extensions;

namespace Wavee.UI.WinUI.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private object? _lastParameterUsed;
    private Frame? _frame;

    public event SharedNavigatedEventHandler? Navigated;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = App.MainWindow.Content as Frame;
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public void SetFrame(object frame)
    {
        if (frame is Frame f)
        {
            Frame = f;
        }
    }

    public void InvokePseudoNavigation(SharedNavigationEventArgs data)
    {
        Navigated?.Invoke(this, data);
    }


    public NavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = _frame.GetPageViewModel();
            _frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    public bool NavigateTo(string? pageKey,
        object? parameter = null, bool clearNavigation = false,
        AnimationType animation = AnimationType.None)
    {

        var pageType = _pageService.GetPageType(pageKey);

        if (_frame != null && (_frame.Content?.GetType() != pageType ||
                               (parameter != null && !parameter.Equals(_lastParameterUsed))))
        {
            _frame.Tag = clearNavigation;
            var vmBeforeNavigation = _frame.GetPageViewModel();
            var navigated = _frame.Navigate(pageType, parameter, animation switch
            {
                AnimationType.None => new SuppressNavigationTransitionInfo(),
                AnimationType.SlideLTR => new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromLeft
                },
                AnimationType.SlideRTL => new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                },
                AnimationType.PopIn => new DrillInNavigationTransitionInfo(),
                _ => throw new ArgumentOutOfRangeException(nameof(animation), animation, null)
            });
            if (navigated)
            {
                _lastParameterUsed = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (frame.GetPageViewModel() is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, new SharedNavigationEventArgs(e.SourcePageType, e.Parameter));
        }
    }
}
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Tango.Types;
using Wavee.UI.Navigation;

namespace Wavee.UI.WinUI;

internal sealed class WinUIFrameFacade : IFrameFacade
{
    private readonly Frame _frame;

    public WinUIFrameFacade(Frame frame)
    {
        _frame = frame;

        _frame.Navigated += (sender, args) =>
        {
            NavigatedTo?.Invoke(this, new FrameNavigatedEventArgs
            {
                View = args.Content,
                ViewModel = args.Parameter is null
                    ? Option<object>.None()
                    : Option<object>.Some(args.Parameter),
                ViewType = args.SourcePageType
            });
        };
    }

    public void NavigateTo(Type pageType, Option<object> viewModel, NavigationAnimationType animationType)
    {
        object? vmUnwrapped = null;
        viewModel.Match(
            actionWhenNone: () => { },
            actionWhenSome: x => { vmUnwrapped = x; }
        );

        _frame.Navigate(pageType, vmUnwrapped, infoOverride: animationType switch
        {
            NavigationAnimationType.None => new SuppressNavigationTransitionInfo(),
            NavigationAnimationType.SlideFromLeft => new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromLeft
            },
            NavigationAnimationType.SlideFromRight => new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            },
            NavigationAnimationType.DrillIn => new DrillInNavigationTransitionInfo(),
        });
    }

    public event EventHandler<FrameNavigatedEventArgs> NavigatedTo;
}
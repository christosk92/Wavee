using System;
using System.Threading.Tasks;
using AnimatedVisuals;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Behaviors.AnimatedVisualPlayer;

public sealed class SetAndLoopSourceBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer>
{
    public static readonly DependencyProperty LetsPlayProperty = DependencyProperty.Register(nameof(LetsPlay), typeof(bool),
        typeof(SetAndLoopSourceBehavior), new PropertyMetadata(true, PropertyChangedCallback));

    private static async void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (SetAndLoopSourceBehavior)d;
        if (e.NewValue is bool v)
        {
            if (v)
            {
                await x.Play();
            }
            else
            {
                x.Stop();
            }
        }
    }

    protected override async void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        await Play();
    }

    private async Task Play()
    {
        if (AssociatedObject is null) return;
        if (AssociatedObject.Source is null) return;

        await AssociatedObject.PlayAsync(0, 1, true);
    }

    private void Stop()
    {
        if (AssociatedObject is null) return;
        AssociatedObject.Stop();
    }

    public bool LetsPlay
    {
        get => (bool)GetValue(LetsPlayProperty);
        set => SetValue(LetsPlayProperty, value);
    }
}
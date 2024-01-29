using System;
using System.Threading.Tasks;
using AnimatedVisuals;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Behaviors.AnimatedVisualPlayer;

public sealed class SetAndLoopSourceBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer>
{
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
}
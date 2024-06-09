using System;
using CommunityToolkit.WinUI.Animations;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;


namespace Wavee.UI.WinUI.Controls.Cards;

[DependencyProperty<string>("Title")]
[DependencyProperty<string>("Description")]
[DependencyProperty<string>("Image")]
[DependencyProperty<string>("Color")]
public sealed partial class AlbumCard : UserControl
{
    public AlbumCard()
    {
        this.InitializeComponent();
        AnimationBuilder.Create().Opacity(
            from: 1,
            to: 0,
            duration: TimeSpan.FromMilliseconds(1)).Start(ActualImageBox);
    }

    private async void ActualImageBox_OnImageOpened(object sender, RoutedEventArgs e)
    {
        //ImageNotloadedGrid.Visibility = Visibility.Collapsed;
        await AnimationBuilder.Create().Opacity(
            from: 0,
            to: 1,
            duration: TimeSpan.FromMilliseconds(400),
            easingType: EasingType.Sine,
            easingMode: EasingMode.EaseInOut).StartAsync(ActualImageBox);
        ImageNotloadedGrid.Visibility = Visibility.Collapsed;
    }
}
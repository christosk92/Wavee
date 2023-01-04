using Eum.UI.ViewModels.Playback;
using Eum.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Eum.UWP.Controls;

partial class PlaylistStyles : ResourceDictionary
{
    public PlaylistStyles()
    {
        InitializeComponent();
    }
    private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var uiElement = (sender as FrameworkElement).Tag;

        if (uiElement is IdWithTitle d)
        {
            Commands.ToAlbum.Execute(d.Id);
        }
    }
}

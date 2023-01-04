using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.ViewModels.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Eum.UI.WinUI.Controls;

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
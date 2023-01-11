// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using CommunityToolkit.WinUI.UI;
using Eum.Logging;
using Eum.UI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Controls
{
    public sealed partial class LyricsControl : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(LyricsViewModel), typeof(LyricsControl), new PropertyMetadata(default(LyricsViewModel?)));


        public LyricsControl()
        {
            this.InitializeComponent();
        }


        public LyricsViewModel? ViewModel
        {
            get => (LyricsViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public bool Negate(bool b)
        {
            return !b;
        }

        private async void Lyrics_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Lyrics.SelectedIndex != -1)
            {
                try
                {
                    S_Log.Instance.LogInfo($"Navigating to {Lyrics.SelectedIndex}");
                    await Lyrics.SmoothScrollIntoViewWithIndexAsync(Lyrics.SelectedIndex, ScrollItemPlacement.Top, false,
                        true, 0, 0);
                }
                catch (Exception x)
                {

                }
            }
        }

        private void LyricsControl_OnUnloaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

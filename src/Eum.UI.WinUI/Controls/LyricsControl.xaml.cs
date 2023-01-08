// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.ViewModels.Playback;
using Nito.AsyncEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Controls
{
    public sealed partial class LyricsControl : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(ItemId?), typeof(LyricsControl), new PropertyMetadata(default(ItemId?), PropertyChangedCallback));
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private static async void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricsControl lc)
            {
                if (e.NewValue is ItemId actualId)
                {
                    if (e.OldValue is ItemId oldId)
                    {
                        if(oldId == actualId) return;
                    }
                    lc._cts.Cancel();
                    lc._cts.Dispose();
                    lc._cts = new CancellationTokenSource();
                    await lc.ViewModel.TryFetchLyrics(actualId, lc._cts.Token);
                }
                else
                {
                    lc.ViewModel.HasLyrics = false;
                }
            }
        }

        public LyricsControl()
        {
            ViewModel = new LyricsViewModel(Ioc.Default.GetRequiredService<ILyricsProvider>(), Ioc.Default.GetRequiredService<PlaybackViewModel>());
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public ItemId? Item
        {
            get => (ItemId?)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public LyricsViewModel ViewModel { get; }

        public bool Negate(bool b)
        {
            return !b;
        }

        private async void Lyrics_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (Lyrics.SelectedItem != null)
            {
                try
                {
                    await Lyrics.SmoothScrollIntoViewWithItemAsync(Lyrics.SelectedItem, ScrollItemPlacement.Top, false, true, 0, 0);
                }
                catch (Exception x)
                {

                }
            }
        }
    }
}

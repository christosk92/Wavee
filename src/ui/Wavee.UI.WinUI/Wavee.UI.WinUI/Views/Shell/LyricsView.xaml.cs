using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModels.NowPlaying;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class LyricsView : UserControl
    {
        public LyricsView()
        {
            this.InitializeComponent();

            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

            if (args.NewValue is LyricsViewModel v)
            {
                v.PropertyChanged -= VOnPropertyChanged;
                v.PropertyChanged += VOnPropertyChanged;
            }
        }

        private void VOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LyricsViewModel.ActiveLine)
                || e.PropertyName == nameof(LyricsViewModel.HasLyrics))
            {
                OnActiveLineChanged();
            }
        }

        public LyricsViewModel ViewModel => DataContext is LyricsViewModel lv ? lv : null;

        private void OnListViewLoaded(object sender, RoutedEventArgs e)
        {
            // Scroll to the active line when the ListView loads
            OnActiveLineChanged();
        }

        private void OnActiveLineChanged()
        {
            // Assuming LyricsListView is the x:Name of your ListView
            var index = ViewModel?.ActiveLineIndex;
            if (index is -1 or null)
            {
                var scroller = LyricsListView.FindDescendant<ScrollViewer>();
                if (scroller is not null)
                {
                    scroller.ChangeView(null, 0, null);
                }

                return;
            }
            var item = (UIElement)LyricsListView.ContainerFromIndex(index.Value);
            if (item is null)
            {
                var scroller = LyricsListView.FindDescendant<ScrollViewer>();
                if (scroller is not null)
                {
                    scroller.ChangeView(null, 0, null);
                }
                return;
            }
            //var off = 0;  
            LyricsListView.SmoothScrollIntoViewWithItemAsync(ViewModel.ActiveLine!.Value, ScrollItemPlacement.Top,
                false,
                scrollIfVisible: true,
                additionalHorizontalOffset: 0,
                additionalVerticalOffset: -120);
            LyricsListView.UpdateLayout();
        }

        private void LyricsListView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var sz = e.NewSize.Height - DoubleHeightBorder.ActualHeight;
            if (sz is not 0 && sz > 0)
            {
                DoubleHeightBorder.Height = sz;
            }
        }
    }
}

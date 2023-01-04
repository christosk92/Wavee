// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Artists;
using Eum.UWP.Transitions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Artists
{
    public sealed partial class  ArtistRootView : UserControl
    {
        private TransitionHelper transitionHelper = new TransitionHelper
        {
            Configs = new List<TransitionConfig>
            {
                new TransitionConfig
                {
                    Id = "title",
                    ScaleMode = ScaleMode.ScaleX,
                },
                new TransitionConfig
                {
                    Id = "floatpanel",
                    ScaleMode = ScaleMode.None
                },
                new TransitionConfig
                {
                    Id = "bg", TransitionMode = TransitionMode.Image,
                    ScaleMode = ScaleMode.None
                }
            }
        };

        public ArtistRootView(ArtistRootViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = viewModel;
        }

        ~ArtistRootView()
        {
            Bindings.StopTracking();
        }
        public ArtistRootViewModel ViewModel { get; set; }

        private void GridView_SizeCHanged(object sender, SizeChangedEventArgs e)
        {
            var s = (sender as ListView);

            var columns = Math.Clamp(Math.Floor(s.ActualWidth / 300), 1, 2);
            // if (Math.Abs(columns - 1) < 0.001)
            // {
            //     s.MaxHeight = 5 * ((ItemsWrapGrid) s.ItemsPanelRoot).ItemHeight;
            // }
            // else
            // {
            //     s.MaxHeight = Double.PositiveInfinity;
            // }
            ((ItemsWrapGrid)s.ItemsPanelRoot).ItemWidth = e.NewSize.Width / columns;
        }

        private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Scroller.ViewChanged -= Scroller_OnViewChanged;
            Scroller.ViewChanged -= ScrollerOnViewChanged;
            GridView.SizeChanged -= GridView_SizeCHanged;
            foreach (var listView in _l)
            {
                listView.SelectionChanged -= Selector_OnSelectionChanged;
            }

            _l.Clear();
            Bindings.StopTracking();
        }

        // private void ScrollViewer_OnAnchorRequested(ScrollViewer sender, AnchorRequestedEventArgs args)
        // {
        //     if (_selected != null)
        //     {
        //         args.Anchor = _selected;
        //         _selected = null;
        //     }
        // }
        //
        // private void NavigationView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        // {
        //     _selected = sender;
        // }
        //
        // public NavigationView _selected;
        private double _old;

        private void ScrollerOnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (Scroller.VerticalOffset > _old)
            {
                Scroller.ScrollToVerticalOffset(_old);
                Scroller.ViewChanged -= ScrollerOnViewChanged;
            }
        }

        private async void Scroller_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (Scroller.VerticalOffset > Header.ActualHeight / 1.4f)
            {
                _surpassedHeader = true;

                var firstone = Header;
                var secondone = OverlayPanel;

                transitionHelper.Source = firstone;
                transitionHelper.Target = secondone;
                await transitionHelper.StartAsync();
            }
            else if (Scroller.VerticalOffset <= Header.ActualHeight && _surpassedHeader)
            {
                //go back
                _surpassedHeader = false;


                var firstone = Header;
                var secondone = OverlayPanel;

                transitionHelper.Source = secondone;
                transitionHelper.Target = firstone;
                await transitionHelper.StartAsync();
            }
        }

        private bool _surpassedHeader;

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _l.Add((sender as ListView));
            Scroller.ViewChanged -= ScrollerOnViewChanged;
            _old =
                Scroller.VerticalOffset;
            Scroller.ViewChanged += ScrollerOnViewChanged;
        }

        private HashSet<ListView> _l = new();
    }
}

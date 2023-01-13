// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using Eum.UI.ViewModels.Artists;
using Eum.UI.WinUI.Transitions;
using GridView = Microsoft.UI.Xaml.Controls.GridView;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Artists
{
    public sealed partial class ArtistRootView : UserControl
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
            ViewModel.ArtistFetched += ViewModelOnArtistFetched;
            this.InitializeComponent();
            this.DataContext = viewModel;
        }

        private void ViewModelOnArtistFetched(object sender, EventArgs e)
        {
            this.Bindings.Update();
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
            var scr = (sender as ScrollViewer);
            if (scr.VerticalOffset > Header.ActualHeight / 1.4f)
            {
                _surpassedHeader = true;

                var firstone = Header;
                var secondone = OverlayPanel;

                transitionHelper.Source = firstone;
                transitionHelper.Target = secondone;
                await transitionHelper.StartAsync();
            }
            else if (scr.VerticalOffset <= Header.ActualHeight && _surpassedHeader)
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
            Scroller.ViewChanged -= ScrollerOnViewChanged;
            _old =
                Scroller.VerticalOffset;
            Scroller.ViewChanged += ScrollerOnViewChanged;

            var lv = (sender as ListView).Tag as DiscographyGroup;
            if (lv != null)
            {
                lv.SwitchTemplateCommand.Execute(lv);
            }
        }

        public string ToMonthlyListenersString(long val)
        {
            //4,606,650 monthly listeners
            return val.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " monthly listeners";
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Scroller.ViewChanged -= ScrollerOnViewChanged;
            Scroller.ViewChanged -= Scroller_OnViewChanged;
            ViewModel.ArtistFetched -= ViewModelOnArtistFetched;
            GridView.SizeChanged -= GridView_SizeCHanged;
            foreach (var listView in _lvs)
            {
                listView.SelectionChanged -= Selector_OnSelectionChanged;
            }

            _lvs.Clear();
            this.Bindings.StopTracking();
            ViewModel = null;
            this.DataContext = null;
        }

        private HashSet<ListView> _lvs = new HashSet<ListView>();

        private void Lv_OnLoaded(object sender, RoutedEventArgs e)
        {
            _lvs.Add((sender as ListView));
        }
    }
}

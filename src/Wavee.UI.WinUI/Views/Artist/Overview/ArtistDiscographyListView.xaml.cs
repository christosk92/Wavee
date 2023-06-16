using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.Core.Contracts.Artist;
using Microsoft.UI.Input;
using Wavee.UI.WinUI.Extensions;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.WinUI.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist.Overview
{
    public sealed partial class ArtistDiscographyListView : UserControl, IDisposable
    {
        public ArtistDiscographyListView(List<ArtistDiscographyItem> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }
        public List<ArtistDiscographyItem> Items { get; set; }
        // private void ElementFactory_OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        // {
        //     args.TemplateKey = args.DataContext switch
        //     {
        //         ArtistDiscographyTrack => "track",
        //         _ => "regular"
        //     };
        // }

        private void UIElement_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void UIElement_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }

        private void UIElement_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool bCtrlDown = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down));
            if (bCtrlDown && e.Key == VirtualKey.A)
            {
                // var itemsView = (sender as UIElement).FindDescendant<ItemsView>();
                // if (itemsView.SelectionModel.SelectedItems.Count == ((IList)itemsView.ItemsSource).Count)
                // {
                //     itemsView.SelectionModel.ClearSelection();
                //     e.Handled = true;
                // }
                // Mark event as handled

            }
            // if (CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            // {
            //     // Check if the A key is pressed
            //     if (e.Key == VirtualKey.A)
            //     {
            //         // Perform your "Select All" operation here
            //
            //         // Mark event as handled
            //         e.Handled = true;
            //     }
            // }
        }

        public void Dispose()
        {
            foreach (var item in Items)
            {
                item.Tracks.Tracks.Clear();
                item.Tracks = null;
            }
            Items.Clear();
            Items = null;
        }
    }
}

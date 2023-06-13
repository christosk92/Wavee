using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime;
using System.Threading.Tasks;
using System;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels.Playlists;
using Wavee.UI.ViewModels.Playlists.Specific;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Playlist
{
    public sealed partial class PlaylistView : UserControl, INavigateablePage, ICacheablePage
    {
        public PlaylistView()
        {
            this.InitializeComponent();
            var dispatcher = this.DispatcherQueue;
            ViewModel = new PlaylistViewModel(action => dispatcher.TryEnqueue(DispatcherQueuePriority.High, () => action()));
        }
        public PlaylistViewModel ViewModel { get; }
        public async void NavigatedTo(object parameter)
        {
            switch (parameter)
            {
                case PlaylistSidebarItem sidebarItem:
                    await ViewModel.Create(sidebarItem);
                    break;
                case AudioId id:
                    await ViewModel.Create(id);
                    break;
            }
        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 2;
        }

        public void RemovedFromCache()
        {
            ViewModel.Destroy();
            _ = Task.Run(() =>
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });
        }

        private void OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        {
            args.TemplateKey = "regular";
        }

        public Visibility IsBusyVisibility(bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Lv_OnLoaded(object sender, RoutedEventArgs e)
        {
            var scroller = Lv.FindDescendant<ScrollViewer>();

            if (scroller != null)
            {
                scroller.ViewChanging += ScrollerOnViewChanging;
            }
        }

        private void ScrollerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            //check if the scrollviewer is almost at the bottom.
            var scrl = sender as ScrollViewer;
            if (e.NextView.VerticalOffset > (scrl.ScrollableHeight - 100))
            {
                ViewModel.NextPage();
            }
        }

        private void Lv_OnUnloaded(object sender, RoutedEventArgs e)
        {
            var scroller = Lv.FindDescendant<ScrollViewer>();
            if (scroller != null)
            {
                scroller.ViewChanging -= ScrollerOnViewChanging;
            }
        }
    }
}

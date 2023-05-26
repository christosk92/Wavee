using Microsoft.UI.Xaml.Controls;
using System;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.WinUI.UI;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Home
{
    public sealed partial class HomeRootView : UserControl, INavigablePage
    {
        public HomeRootView()
        {
            ViewModel = new HomeViewModel<WaveeUIRuntime>(App.Runtime);
            this.InitializeComponent();
        }

        public bool ShouldKeepInCache(int depth)
        {
            return depth < 10;
        }
        public HomeViewModel<WaveeUIRuntime> ViewModel { get; }
        public void RemovedFromCache()
        {
            
        }

        Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var addedItems = e.AddedItems;
            if (addedItems.Count > 0)
            {
                var addedItem = e.AddedItems[0] as TokenItem;
                switch (addedItem.Tag)
                {
                    case "all":
                        await ViewModel.FetchAll();
                        break;
                    case "songs":
                        await ViewModel.FetchSongsOnly();
                        break;
                    case "podcasts":
                        break;
                }
            }
        }

        public bool NegateBool(bool b)
        {
            return !b;
        }

        private void SpotifyItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag;
            if (tag is not AudioId id)
            {
                return;
            }

            //if the originalSource contains ButtonsPanel, we tapped on a button and we don't want to navigate
            if (e.OriginalSource is FrameworkElement originalSource
                && originalSource.FindAscendantOrSelf<StackPanel>(x => x.Name is "ButtonsPanel") is { })
            {
                return;
            }

            UICommands.NavigateTo.Execute(id);
        }
    }
}

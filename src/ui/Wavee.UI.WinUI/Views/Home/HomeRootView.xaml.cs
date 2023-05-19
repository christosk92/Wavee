using Microsoft.UI.Xaml.Controls;
using System;
using CommunityToolkit.Labs.WinUI;
using LanguageExt;
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
            return depth < 3;
        }
        public HomeViewModel<WaveeUIRuntime> ViewModel { get; }

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
    }
}

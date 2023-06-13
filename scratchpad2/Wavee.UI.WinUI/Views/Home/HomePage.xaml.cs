using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Home
{
    public sealed partial class HomePage : UserControl, INavigable, ICacheablePage
    {
        public HomePage()
        {
            ViewModel = new HomeViewModel();
            this.InitializeComponent();
        }

        public HomeViewModel ViewModel { get; }
        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = ((TokenView)sender).SelectedIndex;
            if (ViewModel is not null)
                await ViewModel.FetchView((HomeViewType)selectedIndex);
        }
        public bool NegateBool(bool b)
        {
            return !b;
        }

        private void OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs e)
        {
            if (e.DataContext is CardItem item)
            {
                e.TemplateKey = item.Id.Type switch
                {
                    AudioItemType.Artist => "artist",
                    _ => "regular"
                };
                //e.TemplateKey = (item.Index % 2 == 0) ? "even" : "odd";
            }
        }

        public void NavigatedTo(object parameter)
        {

        }

        public void NavigatedFrom()
        {

        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 10;
        }

        public void RemovedFromCache()
        {

        }
    }
}

using Windows.UI.Xaml;

namespace Eum.UWP.Controls
{
    partial class SidebarControlStyles : ResourceDictionary
    {
        public SidebarControlStyles()
        {
            InitializeComponent();
        }

        // private void Second_ItemsREpeaterLoaded(object sender, RoutedEventArgs e)
        // {
        //
        //     var itemsRepeater = (sender as ItemsRepeater);
        //     var ascendant = itemsRepeater.FindAscendant<SidebarControl>();
        //     itemsRepeater.ItemsSource = ascendant.ViewModel.Playlists;
        // }
    }
}

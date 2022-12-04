using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Controls
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
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
    public class SortingButton : Button
    {
        public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.Register(nameof(SortDirection), typeof(SortDirection), typeof(SortingButton), new PropertyMetadata(default(SortDirection)));
        public static readonly DependencyProperty IsSortingProperty = DependencyProperty.Register(nameof(IsSorting), typeof(bool), typeof(SortingButton), new PropertyMetadata(default(bool)));

        public SortDirection SortDirection
        {
            get => (SortDirection) GetValue(SortDirectionProperty);
            set => SetValue(SortDirectionProperty, value);
        }

        public bool IsSorting
        {
            get => (bool) GetValue(IsSortingProperty);
            set => SetValue(IsSortingProperty, value);
        }
    }
}

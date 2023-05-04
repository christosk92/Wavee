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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.Shell.Sidebar;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Shell.Sidebar
{
    public sealed partial class CountedSidebarView : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(CountedSidebarItem), typeof(CountedSidebarView), new PropertyMetadata(default(CountedSidebarItem)));
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen),
            typeof(bool), typeof(CountedSidebarView),
            new PropertyMetadata(default(bool), IsOpenChanged));


        public CountedSidebarView()
        {
            this.InitializeComponent();
        }

        public CountedSidebarItem Item
        {
            get => (CountedSidebarItem)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private void ChangeOpenState(bool b)
        {
            
        }

        private static void IsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dObj = (CountedSidebarView)d;
            dObj.ChangeOpenState(e.NewValue is bool and true);
        }

    }
}

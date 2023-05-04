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
    public sealed partial class GeneralSidebarView : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(GeneralSidebarItem), typeof(GeneralSidebarView), new PropertyMetadata(default(GeneralSidebarItem)));

        public GeneralSidebarView()
        {
            this.InitializeComponent();
        }

        public GeneralSidebarItem Item
        {
            get => (GeneralSidebarItem)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }
    }
}

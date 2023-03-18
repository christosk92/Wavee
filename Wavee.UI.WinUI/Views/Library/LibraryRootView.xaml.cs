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
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.UI.ViewModels.Libray;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Library
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryRootView : Page
    {
        public LibraryRootView()
        {
            ViewModel = Ioc.Default.GetRequiredService<LibraryRootViewModel>();
            this.InitializeComponent();
        }
        public LibraryRootViewModel ViewModel { get; }
    }
}

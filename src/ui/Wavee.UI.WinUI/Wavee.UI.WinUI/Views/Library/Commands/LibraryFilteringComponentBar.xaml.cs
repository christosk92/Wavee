using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModels.Library;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Library.Commands
{
    public sealed partial class LibraryFilteringComponentBar : UserControl
    {
        public LibraryFilteringComponentBar(ILibraryComponentViewModel libraryComponentViewModel)
        {
            Component = libraryComponentViewModel;
            this.InitializeComponent();
        }

        public ILibraryComponentViewModel Component { get; }

        public bool Negate(bool b) => !b;
    }
}

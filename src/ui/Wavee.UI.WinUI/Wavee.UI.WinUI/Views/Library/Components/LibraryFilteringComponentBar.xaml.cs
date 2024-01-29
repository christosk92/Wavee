using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Library.Components
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

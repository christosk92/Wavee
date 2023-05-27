using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibrarySongsView : UserControl
{
    public LibrarySongsView()
    {
        ViewModel = new LibrarySongsViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public LibrarySongsViewModel<WaveeUIRuntime> ViewModel { get; }
}
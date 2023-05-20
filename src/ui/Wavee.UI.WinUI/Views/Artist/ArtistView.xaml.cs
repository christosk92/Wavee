using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistView : UserControl, INavigablePage
{
    public ArtistView()
    {
        ViewModel = new ArtistViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public ArtistViewModel<WaveeUIRuntime> ViewModel { get; }

    Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

    public bool ShouldKeepInCache(int depth)
    {
        //only 1 down (navigating to album)
        return depth <= 1;
    }

}
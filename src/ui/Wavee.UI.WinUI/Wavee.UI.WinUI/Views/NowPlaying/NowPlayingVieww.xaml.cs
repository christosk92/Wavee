using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.WinUI.Views.NowPlaying;

public sealed partial class NowPlayingView : UserControl
{
    public NowPlayingView(NowPlayingViewModel vm)
    {
        this.InitializeComponent();
    }
}
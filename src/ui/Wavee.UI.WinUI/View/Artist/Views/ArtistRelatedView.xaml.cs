using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Client.Artist;
namespace Wavee.UI.WinUI.View.Artist.Views;

public sealed partial class ArtistRelatedView : UserControl
{
    public ArtistRelatedView(WaveeUIArtistView waveeUiArtistView)
    {
        Artist = waveeUiArtistView;
        this.InitializeComponent();
    }
    public WaveeUIArtistView Artist { get; }
}
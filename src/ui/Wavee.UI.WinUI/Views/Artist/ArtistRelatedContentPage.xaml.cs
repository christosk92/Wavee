using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Artist.ViewModels;

// To learn more about WinUI, the WinUI project structure,
namespace Wavee.UI.WinUI.Views.Artist;

public sealed partial class ArtistRelatedContentPage : UserControl
{
    public ArtistRelatedContentPage(ArtistRelatedContentViewModel artistRelatedContentViewModel)
    {
        this.InitializeComponent();
    }
}
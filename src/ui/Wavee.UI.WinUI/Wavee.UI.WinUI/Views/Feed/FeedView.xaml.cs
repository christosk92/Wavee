using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Feed;
namespace Wavee.UI.WinUI.Views.Feed;

public sealed partial class FeedView : UserControl
{
    public FeedView(FeedViewModel feedViewModel)
    {
        this.InitializeComponent();
    }
}
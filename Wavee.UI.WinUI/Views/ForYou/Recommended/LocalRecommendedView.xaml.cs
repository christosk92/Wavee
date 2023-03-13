using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.ForYou.Recommended;

namespace Wavee.UI.WinUI.Views.ForYou.Recommended;

public sealed partial class LocalRecommendedView : UserControl
{
    public LocalRecommendedView(LocalRecommendedViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }
    public LocalRecommendedViewModel ViewModel { get; }
}
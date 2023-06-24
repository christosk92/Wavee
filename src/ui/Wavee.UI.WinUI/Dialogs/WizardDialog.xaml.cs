using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.WinUI.Dialogs;
public sealed partial class WizardDialog : ContentDialog
{
    public WizardDialog(WizardViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        this.ViewModel.PropertyChanging += ViewModel_PropertyChanging;
        this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.Initialize();
    }

    private IWizardViewModel? _previousView;
    private void ViewModel_PropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(WizardViewModel.CurrentView))
        {
            _previousView = ViewModel.CurrentView;
        }
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WizardViewModel.CurrentView))
        {
            var vm = ViewModel.CurrentView;
            var pageType = ViewFactory.GetTypeFromViewModel(vm);
            var goingTo = ViewModel.CurrentView.Index;
            var goingBack = _previousView != null && _previousView.Index > goingTo;

            var transition = goingTo == 0 ? (NavigationTransitionInfo)new EntranceNavigationTransitionInfo()
                : new SlideNavigationTransitionInfo
                {
                    Effect = goingBack ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft
                };
            SetupFrame.Navigate(pageType, null, transition);
        }
    }

    public WizardViewModel ViewModel { get; }

    private void SecondaryButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {

    }

    private void PrimaryButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {

    }
}

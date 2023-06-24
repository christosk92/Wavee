using Microsoft.UI.Xaml;
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

            var transition = (goingTo == 0 && !goingBack) ? (NavigationTransitionInfo)new EntranceNavigationTransitionInfo()
                : new SlideNavigationTransitionInfo
                {
                    Effect = goingBack ? SlideNavigationTransitionEffect.FromLeft : SlideNavigationTransitionEffect.FromRight
                };
            SetupFrame.Navigate(pageType, vm, transition);
        }
    }

    public WizardViewModel ViewModel { get; }

    public double PlusOne(double d)
    {
        return d + 1;
    }

    public Visibility ToVisibility(bool b)
    {
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility IsNotNull(IWizardViewModel s)
    {
        return !string.IsNullOrEmpty(s.SecondaryActionTitle) ? Visibility.Visible : Visibility.Collapsed;
    }

    public string FormatStepOf(double d)
    {
        var step = (int)d + 1;
        return $"Step {step} of {ViewModel.TotalSteps}";
    }
}

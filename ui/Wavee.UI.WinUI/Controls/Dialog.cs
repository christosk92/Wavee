using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;

namespace Wavee.UI.WinUI.Controls
{
    [DependencyProperty<bool>("IsDialogOpen", DefaultValue = false, Category = "Dialog", OnChanged = nameof(OnIsDialogOpenChanged))]
    [DependencyProperty<bool>("IsBusy", DefaultValue = false, Category = "Dialog", OnChanged = nameof(OnIsBusyChanged))]
    [DependencyProperty<bool>("ShowAlert", DefaultValue = false, Category = "Dialog", OnChanged = nameof(OnShowAlertChanged))]
    [DependencyProperty<bool>("IsBackEnabled", DefaultValue = false, Category = "Controls")]
    [DependencyProperty<bool>("IsCancelEnabled", DefaultValue = false, Category = "Controls")]
    [DependencyProperty<bool>("EnableCancelOnPressed", DefaultValue = false, Category = "Controls")]
    [DependencyProperty<bool>("EnableCancelOnEscape", DefaultValue = false, Category = "Controls")]
    [DependencyProperty<double>("MaxContentHeight", DefaultValue = double.PositiveInfinity, Category = "Sizing")]
    [DependencyProperty<double>("MaxContentWidth", DefaultValue = double.PositiveInfinity, Category = "Sizing")]
    [DependencyProperty<double>("IncreasedWidthThreshold", DefaultValue = double.NaN, Category = "Sizing")]
    [DependencyProperty<double>("IncreasedHeightThreshold", DefaultValue = double.NaN, Category = "Sizing")]
    [DependencyProperty<double>("FullScreenHeightThreshold", DefaultValue = double.NaN, Category = "Sizing")]
    [DependencyProperty<bool>("FullScreenEnabled", DefaultValue = false, Category = "Dialog")]
    [DependencyProperty<bool>("IncreasedWidthEnabled", DefaultValue = false, Category = "Sizing")]
    [DependencyProperty<bool>("IncreasedHeightEnabled", DefaultValue = false, Category = "Sizing")]
    [DependencyProperty<bool>("IncreasedSizeEnabled", DefaultValue = false, Category = "Sizing")]
    // Template Binding Properties
    [DependencyProperty<Brush>("OverlayBackground", DefaultValue = "Transparent")]
    [DependencyProperty<Brush>("DialogBackground", DefaultValue = "White")]
    [DependencyProperty<Thickness>("DialogMargin", DefaultValue = "20,30,20,20")]
    [DependencyProperty<Thickness>("BorderMargin", DefaultValue = "40")]
    [DependencyProperty<Thickness>("ContentMargin", DefaultValue = "16,28,16,16")]
    [DependencyProperty<CornerRadius>("CornerRadius", DefaultValue = "8")]
    [DependencyProperty<Thickness>("IncreasedWidthMargin", DefaultValue = "0,40,0,0")]
    [DependencyProperty<Thickness>("IncreasedHeightMargin", DefaultValue = "40,0,0,0")]
    [DependencyProperty<Thickness>("IncreasedSizeMargin", DefaultValue = "0")]
    [DependencyProperty<Thickness>("FullScreenMargin", DefaultValue = "0,30,0,0")]
    public sealed partial class CustomDialog : ContentControl
    {
        private bool _canCancelOpenedOnPointerPressed;
        private bool _canCancelActivatedOnPointerPressed;

        public CustomDialog()
        {
            this.DefaultStyleKey = typeof(CustomDialog);

            this.Loaded += CustomDialog_Loaded;
            this.Unloaded += CustomDialog_Unloaded;
            this.KeyDown += Dialog_KeyDown;
            this.PointerPressed += Dialog_PointerPressed;
            this.SizeChanged += Dialog_SizeChanged;

            // Subscribe to window activation events
            var window = App.ActualWindow; // Replace with your method to get the main window
            if (window != null)
            {
                window.Activated += Window_Activated;
            }
        }

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateVisualStates();
        }

        private void CustomDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            var window = App.ActualWindow;
            if (window != null)
            {
                window.Activated -= Window_Activated;
            }
        }

        private void OnIsDialogOpenChanged(bool oldValue, bool newValue)
        {
            UpdateVisualStates();

            if (newValue)
            {
                Focus(FocusState.Programmatic);
                _ = UpdateOpenedDelayAsync(true);
            }
        }

        private void OnIsBusyChanged(bool oldValue, bool newValue)
        {
            UpdateVisualStates();
        }

        private void OnShowAlertChanged(bool oldValue, bool newValue)
        {
            UpdateVisualStates();
        }

        private void UpdateVisualStates()
        {
            if (IsDialogOpen)
            {
                VisualStateManager.GoToState(this, "Open", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Closed", true);
            }

            if (IsBusy)
            {
                VisualStateManager.GoToState(this, "BusyState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalState", true);
            }

            if (ShowAlert)
            {
                VisualStateManager.GoToState(this, "AlertState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NoAlertState", true);
            }

            // Handle size states
            if (FullScreenEnabled)
            {
                VisualStateManager.GoToState(this, "FullScreenEnabled", true);
            }
            else if (IncreasedSizeEnabled)
            {
                VisualStateManager.GoToState(this, "IncreasedSizeEnabled", true);
            }
            else if (IncreasedWidthEnabled)
            {
                VisualStateManager.GoToState(this, "IncreasedWidthEnabled", true);
            }
            else if (IncreasedHeightEnabled)
            {
                VisualStateManager.GoToState(this, "IncreasedHeightEnabled", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalSize", true);
            }
        }

        private void Dialog_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsDialogOpen && ShowAlert)
            {
                ShowAlert = false;
            }

            if (IsDialogOpen && EnableCancelOnPressed && !IsBusy && _canCancelOpenedOnPointerPressed && _canCancelActivatedOnPointerPressed)
            {
                var point = e.GetCurrentPoint(this).Position;
                var contentArea = this.GetTemplateChild("ContentPresenter") as FrameworkElement;

                if (contentArea != null)
                {
                    var transform = contentArea.TransformToVisual(this);
                    var contentBounds = transform.TransformBounds(new Rect(0, 0, contentArea.ActualWidth, contentArea.ActualHeight));

                    if (!contentBounds.Contains(point))
                    {
                        e.Handled = true;
                        CloseDialog();
                    }
                }
            }
        }

        private void Dialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsDialogOpen && ShowAlert)
            {
                ShowAlert = false;
            }

            if (e.Key == Windows.System.VirtualKey.Escape && EnableCancelOnEscape && !IsBusy)
            {
                e.Handled = true;
                CloseDialog();
            }
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
            ShowAlert = false;
        }

        private void Dialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = e.NewSize.Width;
            var height = e.NewSize.Height;

            var canIncreasedWidth = !double.IsNaN(IncreasedWidthThreshold) && width < IncreasedWidthThreshold;
            var canIncreasedHeight = !double.IsNaN(IncreasedHeightThreshold) && height < IncreasedHeightThreshold;
            var canGoToFullScreen = !double.IsNaN(FullScreenHeightThreshold) && height < FullScreenHeightThreshold;

            IncreasedWidthEnabled = canIncreasedWidth && !canIncreasedHeight;
            IncreasedHeightEnabled = !canIncreasedWidth && canIncreasedHeight;
            IncreasedSizeEnabled = canIncreasedWidth && canIncreasedHeight;
            FullScreenEnabled = canIncreasedWidth && canGoToFullScreen;

            UpdateVisualStates();
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            var isWindowActivated = e.WindowActivationState == WindowActivationState.CodeActivated || e.WindowActivationState == WindowActivationState.PointerActivated;
            _ = UpdateActivatedDelayAsync(isWindowActivated);
        }

        private async Task UpdateOpenedDelayAsync(bool isDialogOpen)
        {
            _canCancelOpenedOnPointerPressed = false;

            if (isDialogOpen)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                _canCancelOpenedOnPointerPressed = true;
            }
        }

        private async Task UpdateActivatedDelayAsync(bool isWindowActivated)
        {
            if (!isWindowActivated)
            {
                _canCancelActivatedOnPointerPressed = false;
            }

            if (isWindowActivated)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                _canCancelActivatedOnPointerPressed = true;
            }
        }
    }
}

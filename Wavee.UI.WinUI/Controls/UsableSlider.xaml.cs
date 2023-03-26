using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls;
public sealed partial class UsableSlider : Slider
{
    private bool _wasStarted;
    private bool _internalReady;
    public bool DragStarted => (_thumb?.IsDragging is true) || _internalReady;
    public static readonly DependencyProperty RealValueProperty = DependencyProperty.Register(nameof(RealValue),
        typeof(double), typeof(UsableSlider),
        new PropertyMetadata(default(double), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var s = (UsableSlider)d;
        if (s.DragStarted) return;
        if (s._wasStarted)
        {
            s._wasStarted = false;
            return;
        }
        s.Value = e.NewValue is double value ? value : 0;
    }

    private Thumb? _thumb;
    public UsableSlider()
    {
        this.InitializeComponent();
        this.ManipulationMode = ManipulationModes.All;
        this.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(bOpen_PointerPressed), true);
        this.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(bOpen_PointerReleased), true);

    }

    private async void bOpen_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
         await Task.Delay(10);
         OnRealValueChanged?.Invoke(this, this.Value);
         _internalReady = false;
    }

    private void bOpen_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _internalReady = true;
    }

    protected async override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        _internalReady = true;
        base.OnManipulationCompleted(e);
        await Task.Delay(10);
        OnRealValueChanged?.Invoke(this, this.Value);
        _internalReady = false;
    }

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        _internalReady = true;
        base.OnManipulationDelta(e);
    }

    private void ThumbOnDragStarted(object sender, DragStartedEventArgs e)
    {
        _internalReady = true;
    }


    public event EventHandler<double>? OnRealValueChanged;

    public double RealValue
    {
        get => (double)GetValue(RealValueProperty);
        set => SetValue(RealValueProperty, value);
    }
    private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        OnRealValueChanged?.Invoke(this, this.Value);
        _internalReady = false;
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        _internalReady = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        _internalReady = false;
    }

    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);
        _internalReady = false;
    }

    // protected async override void OnTapped(TappedRoutedEventArgs e)
    // {
    //     _internalReady = true;
    //     base.OnTapped(e);
    //     await Task.Delay(20);
    //     OnRealValueChanged?.Invoke(this, this.Value);
    //     _internalReady = false;
    // }
    // protected override void OnPointerPressed(PointerRoutedEventArgs e)
    // {
    //     DragStarted = true;
    //     base.OnPointerPressed(e);
    //     Debug.WriteLine("pointer pressed slider");
    // }
    //
    // protected override void OnPointerMoved(PointerRoutedEventArgs e)
    // {
    //     base.OnPointerMoved(e);
    //     //DragStarted = true;
    //     //Debug.WriteLine("pointer moved slider");
    // }
    //
    // protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    // {
    //     base.OnPointerCanceled(e);
    //     DragStarted = false;
    //     if (DragStarted)
    //     {
    //         OnRealValueChanged?.Invoke(this, this.Value);
    //         DragStarted = false;
    //     }
    //     Debug.WriteLine("pointer canceled slider");
    // }
    //
    // protected override void OnPointerReleased(PointerRoutedEventArgs e)
    // {
    //     base.OnPointerReleased(e);
    //     if (DragStarted)
    //     {
    //         OnRealValueChanged?.Invoke(this, this.Value);
    //         DragStarted = false;
    //     }
    //     Debug.WriteLine("pointer released slider");
    // }
    //
    // protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
    // {
    //     base.OnManipulationStarted(e);
    //     DragStarted = true;
    //     Debug.WriteLine("manipulation started slider");
    // }
    //
    // protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    // {
    //     base.OnManipulationDelta(e);
    //     DragStarted = true;
    //     Debug.WriteLine("manipulation delta slider");
    // }
    //
    // protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
    // {
    //     base.OnManipulationStarting(e);
    //     DragStarted = true;
    //     Debug.WriteLine("manipulation starting slider");
    // }
    //
    // protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    // {
    //     base.OnPointerCaptureLost(e);
    //     //Debug.WriteLine("pointer capturelost slider");
    // }
    //
    // protected override void OnTapped(TappedRoutedEventArgs e)
    // {
    //     DragStarted = true;
    //     Debug.WriteLine("tapped slider");
    //     OnRealValueChanged?.Invoke(this, this.Value);
    //     DragStarted = false;
    //     base.OnTapped(e);
    // }

    private void UsableSlider_OnLoaded(object sender, RoutedEventArgs e)
    {
        _thumb = this.FindDescendant<Thumb>();
        _thumb.DragStarted += ThumbOnDragStarted;
        _thumb.DragCompleted += ThumbOnDragCompleted;
    }


}

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wavee.UI.WinUI.Views.Common;
public class SliderEx : Slider
{
    public event EventHandler SliderManipulationStarted;
    public event EventHandler SliderManipulationCompleted;
    public event EventHandler SliderManipulationMoved;
    private bool IsSliderBeingManpulated
    {
        get
        {
            return this.isContainerHeld || this.isThumbHeld;
        }
    }


    private bool isThumbHeld = false;
    private bool isContainerHeld = false;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var thumb = base.GetTemplateChild("HorizontalThumb") as Thumb;
        if (thumb == null)
        {
            thumb = base.GetTemplateChild("VerticalThumb") as Thumb;
        }
        if (thumb != null)
        {
            thumb.DragStarted += this.Thumb_DragStarted;
            thumb.DragCompleted += this.Thumb_DragCompleted;
            thumb.DragDelta += this.Thumb_DragDelta;
        }

        var sliderContainer = base.GetTemplateChild("SliderContainer") as Grid;
        if (sliderContainer != null)
        {
            sliderContainer.AddHandler(PointerPressedEvent,
                new PointerEventHandler(this.SliderContainer_PointerPressed), true);
            sliderContainer.AddHandler(PointerReleasedEvent,
                new PointerEventHandler(this.SliderContainer_PointerReleased), true);
            sliderContainer.AddHandler(PointerMovedEvent,
                new PointerEventHandler(this.SliderContainer_PointerMoved), true);
        }
    }

    private void SliderContainer_PointerMoved(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        this.InvokeMove();
    }

    private void SliderContainer_PointerReleased(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        this.SetContainerHeld(false);
    }

    private void SliderContainer_PointerPressed(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        this.SetContainerHeld(true);
    }

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        this.InvokeMove();
    }

    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        this.SetThumbHeld(false);
    }

    private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        this.SetThumbHeld(true);
    }

    private void SetThumbHeld(bool held)
    {
        bool wasManipulated = this.IsSliderBeingManpulated;
        this.isThumbHeld = held;
        this.InvokeStateChange(wasManipulated);
    }

    private void SetContainerHeld(bool held)
    {
        bool wasManipulated = this.IsSliderBeingManpulated;
        this.isContainerHeld = held;
        this.InvokeStateChange(wasManipulated);
    }

    private void InvokeMove()
    {
        this.SliderManipulationMoved?.Invoke(this, EventArgs.Empty);
    }

    private void InvokeStateChange(bool wasBeingManipulated)
    {
        if (wasBeingManipulated != this.IsSliderBeingManpulated)
        {
            if (this.IsSliderBeingManpulated)
            {
                this.SliderManipulationStarted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.SliderManipulationCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
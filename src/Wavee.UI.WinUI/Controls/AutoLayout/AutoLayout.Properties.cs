
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Controls.AutoLayout;

partial class AutoLayout
{
    // -- IsReverseZIndex DependencyProperty --
    public static readonly DependencyProperty IsReverseZIndexProperty = DependencyProperty.Register(
        nameof(IsReverseZIndex),
        typeof(bool),
        typeof(AutoLayout),
        new PropertyMetadata(default(bool), propertyChangedCallback: InvalidateArrangeCallback));

    public bool IsReverseZIndex
    {
        get => (bool)GetValue(IsReverseZIndexProperty);
        set => SetValue(IsReverseZIndexProperty, value);
    }

    // -- Orientation DependencyProperty --
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(AutoLayout),
        new PropertyMetadata(default(Orientation), propertyChangedCallback: InvalidateMeasureCallback));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    // -- Spacing DependencyProperty --
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing),
        typeof(double),
        typeof(AutoLayout),
        new PropertyMetadata(default(double), propertyChangedCallback: InvalidateMeasureCallback));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    // -- Justify DependencyProperty --
    public static readonly DependencyProperty JustifyProperty = DependencyProperty.Register(
        nameof(Justify),
        typeof(AutoLayoutJustify),
        typeof(AutoLayout),
        new PropertyMetadata(default(AutoLayoutJustify), propertyChangedCallback: InvalidateArrangeCallback));

    public AutoLayoutJustify Justify
    {
        get => (AutoLayoutJustify)GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    // -- PrimaryAxisAlignment DependencyProperty --
    public static readonly DependencyProperty PrimaryAxisAlignmentProperty = DependencyProperty.Register(
        nameof(PrimaryAxisAlignment),
        typeof(AutoLayoutAlignment),
        typeof(AutoLayout),
        new PropertyMetadata(default(AutoLayoutAlignment), propertyChangedCallback: InvalidateArrangeCallback));

    public AutoLayoutAlignment PrimaryAxisAlignment
    {
        get => (AutoLayoutAlignment)GetValue(PrimaryAxisAlignmentProperty);
        set => SetValue(PrimaryAxisAlignmentProperty, value);
    }

    // -- PrimaryAxisAlignment DependencyProperty --
    public static readonly DependencyProperty CounterAxisAlignmentProperty = DependencyProperty.Register(
        nameof(CounterAxisAlignment),
        typeof(AutoLayoutAlignment),
        typeof(AutoLayout),
        new PropertyMetadata(AutoLayoutAlignment.Stretch, propertyChangedCallback: InvalidateArrangeCallback));

    public AutoLayoutAlignment CounterAxisAlignment
    {
        get => (AutoLayoutAlignment)GetValue(CounterAxisAlignmentProperty);
        set => SetValue(CounterAxisAlignmentProperty, value);
    }

    // -- PrimaryAlignment Attached Property --
    [DynamicDependency(nameof(GetPrimaryAlignment))]
    public static readonly DependencyProperty PrimaryAlignmentProperty = DependencyProperty.RegisterAttached(
        "PrimaryAlignment",
        typeof(AutoLayoutPrimaryAlignment),
        typeof(AutoLayout),
        new PropertyMetadata(default(AutoLayoutPrimaryAlignment), propertyChangedCallback: InvalidateParentAutoLayoutArrangeCallback));

    [DynamicDependency(nameof(GetPrimaryAlignment))]
    public static void SetPrimaryAlignment(DependencyObject element, AutoLayoutPrimaryAlignment value)
    {
        element.SetValue(PrimaryAlignmentProperty, value);
    }

    [DynamicDependency(nameof(GetPrimaryAlignment))]
    public static AutoLayoutPrimaryAlignment GetPrimaryAlignment(DependencyObject element)
    {
        return (AutoLayoutPrimaryAlignment)element.GetValue(PrimaryAlignmentProperty);
    }

    // -- CounterAlignment Attached Property --
    [DynamicDependency(nameof(GetCounterAlignment))]
    public static readonly DependencyProperty CounterAlignmentProperty = DependencyProperty.RegisterAttached(
        "CounterAlignment",
        typeof(AutoLayoutAlignment),
        typeof(AutoLayout),
        new PropertyMetadata(default(AutoLayoutAlignment), propertyChangedCallback: InvalidateParentAutoLayoutArrangeCallback));

    [DynamicDependency(nameof(GetCounterAlignment))]
    public static void SetCounterAlignment(DependencyObject element, AutoLayoutAlignment value)
    {
        element.SetValue(CounterAlignmentProperty, value);
    }

    //[DynamicDependency(nameof(SetCounterAlignment))]
    public static AutoLayoutAlignment GetCounterAlignment(DependencyObject element)
    {
        return (AutoLayoutAlignment)element.GetValue(CounterAlignmentProperty);
    }

    // -- IsIndependentLayout Attached Property --
    [DynamicDependency(nameof(GetIsIndependentLayout))]
    public static readonly DependencyProperty IsIndependentLayoutProperty = DependencyProperty.RegisterAttached(
        "IsIndependentLayout",
        typeof(bool),
        typeof(AutoLayout),
        new PropertyMetadata(default(bool), propertyChangedCallback: InvalidateMeasureCallback));

    [DynamicDependency(nameof(GetIsIndependentLayout))]
    public static void SetIsIndependentLayout(DependencyObject element, bool value)
    {
        element.SetValue(IsIndependentLayoutProperty, value);
    }

    [DynamicDependency(nameof(SetIsIndependentLayout))]
    public static bool GetIsIndependentLayout(DependencyObject element)
    {
        return (bool)element.GetValue(IsIndependentLayoutProperty);
    }

    // -- PrimaryLength Attached Property --
    [DynamicDependency(nameof(GetPrimaryLength))]
    public static readonly DependencyProperty PrimaryLengthProperty = DependencyProperty.RegisterAttached(
        "PrimaryLength",
        typeof(double),
        typeof(AutoLayout),
        new PropertyMetadata(double.NaN, propertyChangedCallback: InvalidateMeasureCallback));

    [DynamicDependency(nameof(GetPrimaryLength))]
    public static void SetPrimaryLength(DependencyObject element, double value)
    {
        element.SetValue(PrimaryLengthProperty, value);
    }

    [DynamicDependency(nameof(SetPrimaryLength))]
    public static double GetPrimaryLength(DependencyObject element)
    {
        return (double)element.GetValue(PrimaryLengthProperty);
    }

    // -- CounterLength Attached Property --
    [DynamicDependency(nameof(GetCounterLength))]
    public static readonly DependencyProperty CounterLengthProperty = DependencyProperty.RegisterAttached(
        "CounterLength",
        typeof(double),
        typeof(AutoLayout),
        new PropertyMetadata(double.NaN, propertyChangedCallback: InvalidateMeasureCallback));

    [DynamicDependency(nameof(GetCounterLength))]
    public static void SetCounterLength(DependencyObject element, double value)
    {
        element.SetValue(CounterLengthProperty, value);
    }

    [DynamicDependency(nameof(SetCounterLength))]
    public static double GetCounterLength(DependencyObject element)
    {
        return (double)element.GetValue(CounterLengthProperty);
    }

    private static void InvalidateMeasureCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.InvalidateMeasure();
        }
    }

    private static void InvalidateArrangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.InvalidateArrange();
        }
    }
    private static void InvalidateParentAutoLayoutArrangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement { Parent: AutoLayout al })
        {
            al.InvalidateArrange();
        }
    }
}

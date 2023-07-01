using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace Wavee.UI.WinUI.Panels;

public class NonVirtualizingStackLayout : NonVirtualizingLayout
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(NonVirtualizingStackLayout), new PropertyMetadata(default(Orientation)));
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(NonVirtualizingStackLayout), new PropertyMetadata(default(double)));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(
           NonVirtualizingLayoutContext context,
           Size availableSize)
    {
        var extentU = 0.0;
        var extentV = 0.0;
        var childCount = context.Children.Count;
        var isVertical = Orientation == Orientation.Vertical;
        var spacing = Spacing;
        var constraint = isVertical ?
            availableSize.WithHeight(double.PositiveInfinity) :
            availableSize.WithWidth(double.PositiveInfinity);

        for (var i = 0; i < childCount; ++i)
        {
            var element = context.Children[i];

            if (element.Visibility is Visibility.Collapsed)
            {
                continue;
            }

            element.Measure(constraint);

            if (isVertical)
            {
                extentU += element.DesiredSize.Height;
                extentV = Math.Max(extentV, element.DesiredSize.Width);
            }
            else
            {
                extentU += element.DesiredSize.Width;
                extentV = Math.Max(extentV, element.DesiredSize.Height);
            }

            if (i < childCount - 1)
            {
                extentU += spacing;
            }
        }

        return isVertical ? new Size(extentV, extentU) : new Size(extentU, extentV);
    }

    protected override Size ArrangeOverride(
        NonVirtualizingLayoutContext context,
        Size finalSize)
    {
        var u = 0.0;
        var childCount = context.Children.Count;
        var isVertical = Orientation == Orientation.Vertical;
        var spacing = Spacing;
        var bounds = new Rect();

        for (var i = 0; i < childCount; ++i)
        {
            var element = context.Children[i];

            if (element.Visibility is Visibility.Collapsed)
            {
                continue;
            }

            bounds = isVertical ?
                LayoutVertical(element as FrameworkElement, u, finalSize) :
                LayoutHorizontal(element as FrameworkElement, u, finalSize);
            element.Arrange(bounds);
            u = (isVertical ? bounds.Bottom : bounds.Right) + spacing;
        }

        return new Size(
            Math.Max(finalSize.Width, bounds.Width),
            Math.Max(finalSize.Height, bounds.Height));
    }

    private static Rect LayoutVertical(FrameworkElement element, double y, Size constraint)
    {
        var x = 0.0;
        var width = element.DesiredSize.Width;

        switch (element.HorizontalAlignment)
        {
            case HorizontalAlignment.Center:
                x += (constraint.Width - element.DesiredSize.Width) / 2;
                break;
            case HorizontalAlignment.Right:
                x += constraint.Width - element.DesiredSize.Width;
                break;
            case HorizontalAlignment.Stretch:
                width = constraint.Width;
                break;
        }

        return new Rect(x, y, width, element.DesiredSize.Height);
    }

    private static Rect LayoutHorizontal(FrameworkElement element, double x, Size constraint)
    {
        var y = 0.0;
        var height = element.DesiredSize.Height;

        switch (element.VerticalAlignment)
        {
            case VerticalAlignment.Center:
                y += (constraint.Height - element.DesiredSize.Height) / 2;
                break;
            case VerticalAlignment.Bottom:
                y += constraint.Height - element.DesiredSize.Height;
                break;
            case VerticalAlignment.Stretch:
                height = constraint.Height;
                break;
        }

        return new Rect(x, y, element.DesiredSize.Width, height);
    }
}
public static class RectExtensions
{
    public static Size WithWidth(this Size rect, double width)
    {
        return new Size(width, rect.Height);
    }

    public static Size WithHeight(this Size rect, double height)
    {
        return new Size(rect.Width, height);
    }
}
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Controls.AutoLayout
{
    internal static class AutoLayoutExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetLength(this Size size, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => size.Width,
                Orientation.Vertical => size.Height,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetLength(this Thickness thickness, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => thickness.Left + thickness.Right,
                Orientation.Vertical => thickness.Top + thickness.Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFinite(this double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetPrimaryLength(this FrameworkElement frameworkElement, Orientation orientation)
        {
            // Check first if a PrimaryLength attached property is set on the element
            // (legacy stuff - not really used anymore because it's too verbose and difficult to understand)
            var primaryLength = AutoLayout.GetPrimaryLength(frameworkElement);
            if (primaryLength.IsFinite())
            {
                return primaryLength;
            }

            // Fallback to width or height depending on the orientation
            return orientation switch
            {
                Orientation.Horizontal => frameworkElement.Width,
                Orientation.Vertical => frameworkElement.Height,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetStartLength(this Thickness thickness, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => thickness.Left,
                Orientation.Vertical => thickness.Top,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetCounterStartLength(this Thickness thickness, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => thickness.Top,
                Orientation.Vertical => thickness.Left,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetEndLength(this Thickness thickness, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => thickness.Right,
                Orientation.Vertical => thickness.Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetMinLength(this FrameworkElement frameworkElement, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => frameworkElement.MinWidth,
                Orientation.Vertical => frameworkElement.MinHeight,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetMaxLength(this FrameworkElement frameworkElement, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Horizontal => frameworkElement.MaxWidth,
                Orientation.Vertical => frameworkElement.MaxHeight,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetCounterLength(this Size size, Orientation orientation)
        {
            return size.GetLength(orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetCounterLength(this Thickness thickness, Orientation orientation)
        {
            return thickness.GetLength(orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
        }

        internal static HorizontalAlignment ToHorizontalAlignment(this AutoLayoutAlignment layoutAlignment)
        {
            return layoutAlignment switch
            {
                AutoLayoutAlignment.Start => HorizontalAlignment.Left,
                AutoLayoutAlignment.Center => HorizontalAlignment.Center,
                AutoLayoutAlignment.End => HorizontalAlignment.Right,
                AutoLayoutAlignment.Stretch => HorizontalAlignment.Stretch,
                _ => throw new ArgumentException($"Unknown {nameof(AutoLayoutAlignment)}: {layoutAlignment}", nameof(layoutAlignment)),
            };
        }

        internal static VerticalAlignment ToVerticalAlignment(this AutoLayoutAlignment layoutAlignment)
        {
            return layoutAlignment switch
            {
                AutoLayoutAlignment.Start => VerticalAlignment.Top,
                AutoLayoutAlignment.Center => VerticalAlignment.Center,
                AutoLayoutAlignment.End => VerticalAlignment.Bottom,
                AutoLayoutAlignment.Stretch => VerticalAlignment.Stretch,
                _ => throw new ArgumentException($"Unknown {nameof(AutoLayoutAlignment)}: {layoutAlignment}", nameof(layoutAlignment)),
            };
        }
    }
}

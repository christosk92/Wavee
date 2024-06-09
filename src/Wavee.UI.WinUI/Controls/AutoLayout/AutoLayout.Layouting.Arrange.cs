using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.WinUI.Extensions;

namespace Wavee.UI.WinUI.Controls.AutoLayout;
partial class AutoLayout
{
    private bool _isArranging;

    protected override Size ArrangeOverride(Size finalSize)
    {
        _isArranging = true;

        try
        {
            return InnerArrange(finalSize);
        }
        finally
        {
            _isArranging = false;
        }
    }

    private Size InnerArrange(in Size finalSize)
    {
        // Local cache of DependencyProperties
        var children = Children;
        var orientation = Orientation;
        var justify = Justify;
        var primaryAxisAlignment = PrimaryAxisAlignment;
        var padding = Padding;
        var spacing = Spacing.FiniteOrDefault(0d);
        var isHorizontal = orientation == Orientation.Horizontal;
        var borderThickness = BorderBrush is null ? default : BorderThickness;

        var borderThicknessLength = borderThickness.GetLength(orientation);

        var totalNonFilledStackedSize = 0d;
        var totalOfFillMaxSize = 0d;
        var numberOfFilledChildren = 0;
        var numberOfFilledChildrenWithMax = 0;
        var numberOfStackedChildren = 0;
        var haveStartPadding = false;
        var haveEndPadding = false;
        var startPadding = isHorizontal ? padding.Left : padding.Top;
        var endPadding = isHorizontal ? padding.Right : padding.Bottom;

        if (_calculatedChildren is null || children.Count != _calculatedChildren.Length)
        {
            // If the panel has not been measured yet, we need to measure it now.
            InvalidateMeasure();
            return finalSize;
        }

        if (finalSize != DesiredSize)
        {
            // The _calculatedChildren is calculated for the available size, so we need to re-measure it
            // using the new final size.
            MeasureOverride(finalSize);
        }

        // 1. Calculate the total size of non-filled and filled children
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var calculatedChild = _calculatedChildren[i];

            if (child is FrameworkElement { Visibility: Visibility.Collapsed })
            {
                continue;
            }

            if (!ReferenceEquals(child, calculatedChild.Element))
            {
                // Invalid calculated child, invalidate measure and wait for next pass to fix this.
                InvalidateMeasure();
                return finalSize;
            }

            switch (calculatedChild.Role)
            {
                case AutoLayoutRole.Independent:
                    continue; // Independent children are not stacked, skip it from the calculation
                case AutoLayoutRole.Filled:
                    numberOfFilledChildren++;
                    if (calculatedChild.MeasuredLength < double.MaxValue)
                    {
                        totalOfFillMaxSize += calculatedChild.MeasuredLength;
                        numberOfFilledChildrenWithMax++;
                    }
                    break;
                case AutoLayoutRole.Hug:
                case AutoLayoutRole.Fixed:
                default:
                    totalNonFilledStackedSize += calculatedChild.MeasuredLength;
                    break;
            }

            numberOfStackedChildren++;
        }

        var atLeastOneFilledChild = numberOfFilledChildren > 0;

        switch (primaryAxisAlignment)
        {
            case AutoLayoutAlignment.Start:
                haveStartPadding = true;
                haveEndPadding = atLeastOneFilledChild || justify == AutoLayoutJustify.SpaceBetween;
                break;
            case AutoLayoutAlignment.End:
                haveStartPadding = atLeastOneFilledChild || justify == AutoLayoutJustify.SpaceBetween;
                haveEndPadding = true;
                break;
            case AutoLayoutAlignment.Center:
                var havePadding = atLeastOneFilledChild || justify == AutoLayoutJustify.SpaceBetween;
                haveStartPadding = havePadding;
                haveEndPadding = havePadding;
                break;
        }

        var applicableStartPadding = haveStartPadding ? isHorizontal ? padding.Left : padding.Top : 0;
        var applicableEndPadding = haveEndPadding ? isHorizontal ? padding.Right : padding.Bottom : 0;

        var totalPaddingSize = applicableStartPadding + applicableEndPadding;

        // Available Size is the final size minus the border thickness and the padding
        var availableSizeForStackedChildren = finalSize.GetLength(orientation) - (borderThicknessLength + totalPaddingSize);
        EnsureZeroFloor(ref availableSizeForStackedChildren);

        // Start the offset at the border + start padding
        var currentOffset = borderThickness.GetStartLength(orientation) + applicableStartPadding;

        // Calculate the defined inter-element spacing size (if any, not taking care of SpaceBetween yet)
        var totalSpacingSize = spacing * (numberOfStackedChildren - 1);

        // Calculate the remaining size for each filled children (if any)
        var filledChildrenSize = atLeastOneFilledChild
            ? (availableSizeForStackedChildren - (totalNonFilledStackedSize + totalSpacingSize)) / numberOfFilledChildren
            : 0;

        var filledMaxSurplus = (filledChildrenSize * numberOfFilledChildrenWithMax) - totalOfFillMaxSize;

        EnsureZeroFloor(ref filledMaxSurplus);

        var filledChildrenSizeAfterMaxSize = numberOfFilledChildren - numberOfFilledChildrenWithMax > 0
            ? filledChildrenSize + (filledMaxSurplus / (numberOfFilledChildren - numberOfFilledChildrenWithMax))
            : filledChildrenSize + GetChildrenLowerThanAllocateSurplus(_calculatedChildren, filledChildrenSize);

        EnsureZeroFloor(ref filledChildrenSizeAfterMaxSize);

        // Establish actual inter-element spacing.
        var spacingOffset = justify == AutoLayoutJustify.SpaceBetween && !atLeastOneFilledChild
            // When SpaceBetween is defined and there's no filled children
            ? ComputeSpaceBetween(availableSizeForStackedChildren, totalNonFilledStackedSize, numberOfStackedChildren)
            // Fallback to the Spacing property
            : spacing;


        var haveMoreFilled = (numberOfFilledChildren - numberOfFilledChildrenWithMax) > 0;

        var primaryAxisAlignmentOffset = PrimaryAxisAlignmentOffsetSize(
            totalOfFillMaxSize,
            primaryAxisAlignment,
            availableSizeForStackedChildren,
            totalNonFilledStackedSize,
            haveMoreFilled,
            spacing,
            numberOfStackedChildren,
            filledChildrenSizeAfterMaxSize,
            startPadding,
            endPadding);

        currentOffset += primaryAxisAlignmentOffset;

        // 3. Arrange children, one by one (in reverse order if IsReverseZIndex is true)
        var isReverse = IsReverseZIndex;
        var start = isReverse ? children.Count - 1 : 0;
        var stop = isReverse ? -1 : children.Count;
        var increment = isReverse ? -1 : 1;
        for (var i = start; i != stop; i += increment)
        {
            var child = _calculatedChildren[i];

            if (child.Role == AutoLayoutRole.Independent)
            {
                // Independent is given all the available space,
                // because it's not stacking with others.
                // No padding is applied to it either.
                child.Element.Arrange(new Rect(default, finalSize));

                continue; // next child, current offset remains unchanged
            }

            if (children[i] is FrameworkElement { Visibility: Visibility.Collapsed })
            {
                continue;
            }

            // Calculate the length of the child (size in the panel orientation)

            // Length depends on the role of the child
            var childLength = child.Role == AutoLayoutRole.Filled
                ? Math.Min(child.MeasuredLength, filledChildrenSizeAfterMaxSize)
                : child.MeasuredLength;

            var offsetRelativeToPadding = currentOffset - (applicableStartPadding + borderThicknessLength);

            if (childLength > availableSizeForStackedChildren - offsetRelativeToPadding)
            {
                // Child is too big, truncate it to the remaining space
                childLength = availableSizeForStackedChildren - offsetRelativeToPadding;
            }

            EnsureZeroFloor(ref childLength);

            // Calculate the position of the child by applying the alignment instructions

            // Check if the child has a local value for CounterAlignment
            if (child.Element.ReadLocalValue(CounterAlignmentProperty) is not AutoLayoutAlignment counterAlignment)
            {
                // If not, use the panel's CounterAXisAlignment property instead
                counterAlignment = CounterAxisAlignment;
            }

            var isStretch = counterAlignment is AutoLayoutAlignment.Stretch;
            var haveCounterStartPadding = isStretch || counterAlignment is AutoLayoutAlignment.Start;
            var counterStartPadding = haveCounterStartPadding ? (isHorizontal ? padding.Top : padding.Left) : 0;

            var haveCounterEndPadding = isStretch || counterAlignment is AutoLayoutAlignment.End;
            var counterEndPadding = haveCounterEndPadding ? (isHorizontal ? padding.Bottom : padding.Right) : 0;

            var counterBorderLength = borderThickness.GetCounterLength(orientation);

            var availableCounterLength = finalSize.GetCounterLength(orientation) - (counterStartPadding + counterEndPadding + counterBorderLength);
            EnsureZeroFloor(ref availableCounterLength);

            var childSize = isHorizontal
                ? new Size(childLength, availableCounterLength)
                : new Size(availableCounterLength, childLength);

            ApplyMinMaxValues(child.Element, orientation, ref childSize);

            var alignmentForCounterAlignmentOffset = counterAlignment is AutoLayoutAlignment.Stretch ? CounterAxisAlignment : counterAlignment;

            var counterAlignmentOffset =
                ComputeCounterAlignmentOffset(alignmentForCounterAlignmentOffset, childSize.GetCounterLength(orientation), availableCounterLength, padding, isHorizontal);

            var calculatedOffset = counterAlignmentOffset + counterStartPadding + borderThickness.GetCounterStartLength(orientation);

            var childOffsetPosition = new Point(
                isHorizontal ? currentOffset : calculatedOffset,
                isHorizontal ? calculatedOffset : currentOffset);

            // Arrange the child to its final position, determined by the calculated offset and size
            child.Element.Arrange(new Rect(childOffsetPosition, childSize));

            // Increment the offset for the next child
            currentOffset += (childLength + spacingOffset);
        }

        return finalSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyMinMaxValues(in UIElement element, in Orientation orientation, ref Size desiredSize)
    {
        if (element is not FrameworkElement frameworkElement)
        {
            return;
        }

        // Cache local values to avoid multiple calls to the DP system
        var width = frameworkElement.Width;
        var height = frameworkElement.Height;
        var maxWidth = frameworkElement.MaxWidth;
        var maxHeight = frameworkElement.MaxHeight;
        var minWidth = frameworkElement.MinWidth;
        var minHeight = frameworkElement.MinHeight;

        var primaryLength = GetPrimaryLength(element);
        var counterLength = GetCounterLength(element);

        // Apply primaryLength and counterLength constraints, if defined
        if (orientation is Orientation.Horizontal)
        {
            if (!double.IsNaN(primaryLength)) desiredSize.Width = primaryLength;
            if (!double.IsNaN(counterLength)) desiredSize.Height = counterLength;
        }
        else
        {
            if (!double.IsNaN(primaryLength)) desiredSize.Height = primaryLength;
            if (!double.IsNaN(counterLength)) desiredSize.Width = counterLength;
        }

        // Apply Width and Height constraints, if defined
        if (!double.IsNaN(width)) desiredSize.Width = width;
        if (!double.IsNaN(height)) desiredSize.Height = height;

        // Apply MaxWidth and MaxHeight constraints, if defined
        if (!double.IsNaN(maxWidth) && desiredSize.Width > maxWidth) desiredSize.Width = maxWidth;
        if (!double.IsNaN(maxHeight) && desiredSize.Height > maxHeight) desiredSize.Height = maxHeight;

        // Apply MinWidth and MinHeight constraints
        if (!double.IsNaN(minWidth) && desiredSize.Width < minWidth) desiredSize.Width = minWidth;
        if (!double.IsNaN(minHeight) && desiredSize.Height < minHeight) desiredSize.Height = minHeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ComputeCounterAlignmentOffset(
        in AutoLayoutAlignment? counterAlignment,
        in double childCounterLength,
        in double availableCounterLength,
        in Thickness padding,
        in bool isHorizontal)
    {
        switch (counterAlignment)
        {
            case AutoLayoutAlignment.Center:
                var counterStartPadding = isHorizontal ? padding.Top : padding.Left;
                var counterEndPadding = isHorizontal ? padding.Bottom : padding.Right;
                var alignmentOffsetSize = availableCounterLength - (counterStartPadding + counterEndPadding);
                var relativeOffset = Math.Abs(alignmentOffsetSize) / 2;

                var test = alignmentOffsetSize > 0 ?
                    (relativeOffset + counterStartPadding) - (childCounterLength / 2) :
                    relativeOffset + (availableCounterLength - counterEndPadding) - (childCounterLength / 2);
                return test;
            case AutoLayoutAlignment.End:
                return availableCounterLength - childCounterLength;
            default:
                return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ComputeSpaceBetween(
        in double availableSizeForStackedChildren,
        in double totalNonFilledStackedSize,
        in int numberOfStackedChildren)
    {
        var availableSizeForStackedSpaceBetween = availableSizeForStackedChildren - totalNonFilledStackedSize;

        EnsureZeroFloor(ref availableSizeForStackedSpaceBetween);

        return availableSizeForStackedSpaceBetween / (numberOfStackedChildren - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double PrimaryAxisAlignmentOffsetSize(
        in double totalOfFillMaxSize,
        in AutoLayoutAlignment autoLayoutAlignment,
        in double availableSizeForStackedChildren,
        in double totalNonFilledStackedSize,
        in bool haveMoreFilled,
        in double spacing,
        in int numberOfStackedChildren,
        in double filledChildrenSizeAfterMaxSize,
        in double startPadding,
        in double endPadding)
    {
        if (haveMoreFilled || autoLayoutAlignment is AutoLayoutAlignment.Start)
        {
            return 0;
        }

        var totalSpacingSize = spacing * (numberOfStackedChildren - 1);

        var fillOverflow = filledChildrenSizeAfterMaxSize > 0 ? totalOfFillMaxSize : filledChildrenSizeAfterMaxSize;

        var occupiedSpace = totalNonFilledStackedSize + totalSpacingSize + fillOverflow;

        switch (autoLayoutAlignment)
        {
            case AutoLayoutAlignment.Center:
                var alignmentOffsetSize = availableSizeForStackedChildren - (startPadding + endPadding);
                var relativeOffset = Math.Abs(alignmentOffsetSize) / 2;

                return alignmentOffsetSize > 0 ?
                    (relativeOffset + startPadding) - (occupiedSpace / 2) :
                    relativeOffset + (availableSizeForStackedChildren - endPadding) - (occupiedSpace / 2);
            case AutoLayoutAlignment.End when filledChildrenSizeAfterMaxSize == 0 || filledChildrenSizeAfterMaxSize > 0:
                return availableSizeForStackedChildren - occupiedSpace;
            default:
                return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetChildrenLowerThanAllocateSurplus(
        in Span<CalculatedChildren> calculatedChildren,
        in double filledChildrenSizeBeforeMaxSize)
    {
        int countFilledChildrenGreaterThanAllocated = 0;
        double sumSurplusFromLower = 0d;

        for (int i = 0; i < calculatedChildren.Length; i++)
        {
            if (calculatedChildren[i].Role is AutoLayoutRole.Filled)
            {
                if (calculatedChildren[i].MeasuredLength > filledChildrenSizeBeforeMaxSize)
                {
                    countFilledChildrenGreaterThanAllocated++;
                }
                else
                {
                    sumSurplusFromLower += filledChildrenSizeBeforeMaxSize - calculatedChildren[i].MeasuredLength;
                }
            }
        }

        return countFilledChildrenGreaterThanAllocated > 0 ? sumSurplusFromLower / countFilledChildrenGreaterThanAllocated : 0;
    }
}

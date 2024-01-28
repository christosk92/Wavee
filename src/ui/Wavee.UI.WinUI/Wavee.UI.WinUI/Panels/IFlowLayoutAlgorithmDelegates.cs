// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Panels
{
    internal struct FlowLayoutAnchorInfo
    {
        public int Index { get; set; }
        public double Offset { get; set; }
    }

    internal interface IFlowLayoutAlgorithmDelegates
    {
        Size Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context);
        Size Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context);
        bool Algorithm_ShouldBreakLine(int index, double remainingSpace);
        FlowLayoutAnchorInfo Algorithm_GetAnchorForRealizationRect(Size availableSize, VirtualizingLayoutContext context);
        FlowLayoutAnchorInfo Algorithm_GetAnchorForTargetElement(int targetIndex, Size availableSize, VirtualizingLayoutContext context);
        Rect Algorithm_GetExtent(
            Size availableSize,
            VirtualizingLayoutContext context,
            UIElement? firstRealized,
            int firstRealizedItemIndex,
            Rect firstRealizedLayoutBounds,
            UIElement? lastRealized,
            int lastRealizedItemIndex,
            Rect lastRealizedLayoutBounds);
        void Algorithm_OnElementMeasured(
            UIElement element,
            int index,
            Size availableSize,
            Size measureSize,
            Size desiredSize,
            Size provisionalArrangeSize,
            VirtualizingLayoutContext context);
        void Algorithm_OnLineArranged(
            int startIndex,
            int countInLine,
            double lineSize,
            VirtualizingLayoutContext context);
    }
}
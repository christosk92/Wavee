using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Wavee.UI.WinUI.Panels.Flow;

namespace Wavee.UI.WinUI.Panels;

/// <summary>
/// Defines constants that specify how items are aligned on the non-scrolling or non-virtualizing axis.
/// </summary>
public enum UniformGridLayoutItemsJustification
{
    /// <summary>
    /// Items are aligned with the start of the row or column, with extra space at the end.
    /// Spacing between items does not change.
    /// </summary>
    Start = 0,

    /// <summary>
    /// Items are aligned in the center of the row or column, with extra space at the start and
    /// end. Spacing between items does not change.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Items are aligned with the end of the row or column, with extra space at the start.
    /// Spacing between items does not change.
    /// </summary>
    End = 2,

    /// <summary>
    /// Items are aligned so that extra space is added evenly before and after each item.
    /// </summary>
    SpaceAround = 3,

    /// <summary>
    /// Items are aligned so that extra space is added evenly between adjacent items. No space
    /// is added at the start or end.
    /// </summary>
    SpaceBetween = 4,

    SpaceEvenly = 5,
};

/// <summary>
/// Defines constants that specify how items are sized to fill the available space.
/// </summary>
public enum UniformGridLayoutItemsStretch
{
    /// <summary>
    /// The item retains its natural size. Use of extra space is determined by the
    /// <see cref="UniformGridLayout.ItemsJustification"/> property.
    /// </summary>
    None = 0,

    /// <summary>
    /// The item is sized to fill the available space in the non-scrolling direction. Item size
    /// in the scrolling direction is not changed.
    /// </summary>
    Fill = 1,

    /// <summary>
    /// The item is sized to both fill the available space in the non-scrolling direction and
    /// maintain its aspect ratio.
    /// </summary>
    Uniform = 2,
};


/// <summary>
/// Positions elements sequentially from left to right or top to bottom in a wrapping layout.
/// </summary>
public class UniformGridLayoutPanel : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
{
    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(nameof(MinItemWidth), typeof(double), typeof(UniformGridLayoutPanel), new PropertyMetadata(double.NaN, PropertyChangedCallback));
    public static readonly DependencyProperty ItemsStretchProperty = DependencyProperty.Register(nameof(ItemsStretch), typeof(UniformGridLayoutItemsStretch), typeof(UniformGridLayoutPanel), new PropertyMetadata(default(UniformGridLayoutItemsStretch), PropertyChangedCallback));
    public static readonly DependencyProperty MinItemHeightProperty = DependencyProperty.Register(nameof(MinItemHeight), typeof(double), typeof(UniformGridLayoutPanel), new PropertyMetadata(double.NaN, PropertyChangedCallback));
    public static readonly DependencyProperty MinRowSpacingProperty = DependencyProperty.Register(nameof(MinRowSpacing), typeof(double), typeof(UniformGridLayoutPanel), new PropertyMetadata(default(double), PropertyChangedCallback));
    public static readonly DependencyProperty ItemsJustificationProperty = DependencyProperty.Register(nameof(ItemsJustification), typeof(UniformGridLayoutItemsJustification), typeof(UniformGridLayoutPanel), new PropertyMetadata(default(UniformGridLayoutItemsJustification), PropertyChangedCallback));
    public static readonly DependencyProperty MinColumnSpacingProperty = DependencyProperty.Register(nameof(MinColumnSpacing), typeof(double), typeof(UniformGridLayoutPanel), new PropertyMetadata(default(double), PropertyChangedCallback));
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(UniformGridLayoutPanel), new PropertyMetadata(Microsoft.UI.Xaml.Controls.Orientation.Horizontal, PropertyChangedCallback));

    private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();
    public static readonly DependencyProperty MaximumRowsOrColumnnsProperty = DependencyProperty.Register(nameof(MaximumRowsOrColumnns), typeof(int), typeof(UniformGridLayoutPanel), new PropertyMetadata(int.MaxValue, PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (UniformGridLayoutPanel)d;
        //Note: For UniformGridLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
        //i.e. the properties are the inverse of each other.
        if (e.Property == OrientationProperty)
        {
            var orientation = (Orientation)e.NewValue;
            var scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal;
            c._orientation.ScrollOrientation = scrollOrientation;
        }

        c.InvalidateLayout();
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="UniformGridLayout"/> class.
    /// </summary>
    public UniformGridLayoutPanel()
    {
        LayoutId = "UniformGridLayout";
    }
    public string? LayoutId { get; set; }

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public UniformGridLayoutItemsStretch ItemsStretch
    {
        get => (UniformGridLayoutItemsStretch)GetValue(ItemsStretchProperty);
        set => SetValue(ItemsStretchProperty, value);
    }

    public double MinItemHeight
    {
        get => (double)GetValue(MinItemHeightProperty);
        set => SetValue(MinItemHeightProperty, value);
    }

    public double MinRowSpacing
    {
        get => (double)GetValue(MinRowSpacingProperty);
        set => SetValue(MinRowSpacingProperty, value);
    }

    public UniformGridLayoutItemsJustification ItemsJustification
    {
        get => (UniformGridLayoutItemsJustification)GetValue(ItemsJustificationProperty);
        set => SetValue(ItemsJustificationProperty, value);
    }

    public double MinColumnSpacing
    {
        get => (double)GetValue(MinColumnSpacingProperty);
        set => SetValue(MinColumnSpacingProperty, value);
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public int MaximumRowsOrColumnns
    {
        get => (int)GetValue(MaximumRowsOrColumnnsProperty);
        set => SetValue(MaximumRowsOrColumnnsProperty, value);
    }

    Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(
          int index,
          Size availableSize,
          VirtualizingLayoutContext context)
    {
        var gridState = (UniformGridLayoutState)context.LayoutState!;
        return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
    }

    Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
        int index,
        Size measureSize,
        Size desiredSize,
        VirtualizingLayoutContext context)
    {
        var gridState = (UniformGridLayoutState)context.LayoutState!;
        return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
    }

    bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace) => remainingSpace < 0;

    FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
        Size availableSize,
        VirtualizingLayoutContext context)
    {
        Rect bounds = new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
        int anchorIndex = -1;

        int itemsCount = context.ItemCount;
        var realizationRect = context.RealizationRect;
        if (itemsCount > 0 && _orientation.MajorSize(realizationRect) > 0)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState!;
            var lastExtent = gridState.FlowAlgorithm.LastExtent;
            var itemsPerLine = Math.Min( // note use of unsigned ints
                Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
                Math.Max(1u, (uint)MaximumRowsOrColumnns));
            var majorSize = (itemsCount / itemsPerLine) * GetMajorSizeWithSpacing(context);
            var realizationWindowStartWithinExtent = _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent);
            if ((realizationWindowStartWithinExtent + _orientation.MajorSize(realizationRect)) >= 0 && realizationWindowStartWithinExtent <= majorSize)
            {
                double offset = Math.Max(0.0, _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent));
                int anchorRowIndex = (int)(offset / GetMajorSizeWithSpacing(context));

                anchorIndex = (int)Math.Max(0, Math.Min(itemsCount - 1, anchorRowIndex * itemsPerLine));
                bounds = GetLayoutRectForDataIndex(availableSize, anchorIndex, lastExtent, context);
            }
        }

        return new FlowLayoutAnchorInfo
        {
            Index = anchorIndex,
            Offset = _orientation.MajorStart(bounds)
        };
    }

    FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
        int targetIndex,
        Size availableSize,
        VirtualizingLayoutContext context)
    {
        int index = -1;
        double offset = double.NaN;
        int count = context.ItemCount;
        if (targetIndex >= 0 && targetIndex < count)
        {
            int itemsPerLine = (int)Math.Min( // note use of unsigned ints
                Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
                Math.Max(1u, MaximumRowsOrColumnns));
            int indexOfFirstInLine = (targetIndex / itemsPerLine) * itemsPerLine;
            index = indexOfFirstInLine;
            var state = (UniformGridLayoutState)context.LayoutState!;
            offset = _orientation.MajorStart(GetLayoutRectForDataIndex(availableSize, indexOfFirstInLine, state.FlowAlgorithm.LastExtent, context));
        }

        return new FlowLayoutAnchorInfo
        {
            Index = index,
            Offset = offset
        };
    }

    Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(
        Size availableSize,
        VirtualizingLayoutContext context,
        UIElement? firstRealized,
        int firstRealizedItemIndex,
        Rect firstRealizedLayoutBounds,
        UIElement? lastRealized,
        int lastRealizedItemIndex,
        Rect lastRealizedLayoutBounds)
    {
        var extent = new Rect();


        // Constants
        int itemsCount = context.ItemCount;
        double availableSizeMinor = _orientation.Minor(availableSize);
        int itemsPerLine =
            (int)Math.Min( // note use of unsigned ints
                Math.Max(1u, !double.IsInfinity(availableSizeMinor)
                    ? (uint)(availableSizeMinor / GetMinorSizeWithSpacing(context))
                    : (uint)itemsCount),
            Math.Max(1u, MaximumRowsOrColumnns));
        double lineSize = GetMajorSizeWithSpacing(context);

        if (itemsCount > 0)
        {
            _orientation.SetMinorSize(
                ref extent,
                !double.IsInfinity(availableSizeMinor) && ItemsStretch == UniformGridLayoutItemsStretch.Fill ?
                availableSizeMinor :
                Math.Max(0.0, itemsPerLine * GetMinorSizeWithSpacing(context) - (double)MinColumnSpacing));
            _orientation.SetMajorSize(
                ref extent,
                Math.Max(0.0, (itemsCount / itemsPerLine) * lineSize - (double)MinRowSpacing));

            if (firstRealized != null)
            {
                _orientation.SetMajorStart(
                    ref extent,
                    _orientation.MajorStart(firstRealizedLayoutBounds) - (firstRealizedItemIndex / itemsPerLine) * lineSize);
                int remainingItems = itemsCount - lastRealizedItemIndex - 1;
                _orientation.SetMajorSize(
                    ref extent,
                    _orientation.MajorEnd(lastRealizedLayoutBounds) - _orientation.MajorStart(extent) + (remainingItems / itemsPerLine) * lineSize);
            }
            else
            {
            }
        }

        return extent;
    }

    void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(UIElement element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
    {
    }

    void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
    {
    }

    protected override void InitializeForContextCore(VirtualizingLayoutContext context)
    {
        var state = context.LayoutState;
        var gridState = state as UniformGridLayoutState;

        if (gridState == null)
        {
            if (state != null)
            {
                throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState.");
            }

            // Custom deriving layouts could potentially be stateful.
            // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
            gridState = new UniformGridLayoutState();
        }

        gridState.InitializeForContext(context, this);
    }
    protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
    {
        var gridState = (UniformGridLayoutState)context.LayoutState!;
        gridState.UninitializeForContext(context);
    }


    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        try
        {
            // Set the width and height on the grid state. If the user already set them then use the preset. 
            // If not, we have to measure the first element and get back a size which we're going to be using for the rest of the items.
            var gridState = (UniformGridLayoutState)context.LayoutState!;
            gridState.EnsureElementSize(availableSize, context, MinItemWidth, MinItemHeight, ItemsStretch, Orientation,
                MinRowSpacing, MinColumnSpacing, MaximumRowsOrColumnns);

            var desiredSize = GetFlowAlgorithm(context).Measure(
                availableSize,
                context,
                true,
                MinColumnSpacing,
                MinRowSpacing,
                MaximumRowsOrColumnns,
                _orientation.ScrollOrientation,
                false,
                LayoutId);

            // If after Measure the first item is in the realization rect, then we revoke grid state's ownership,
            // and only use the layout when to clear it when it's done.
            gridState.EnsureFirstElementOwnership(context);

            return desiredSize;
        }
        catch (COMException c)
        {
            return new Size(0, 0);
        }
        catch (Exception e)
        {
            return new Size(0, 0);
        }
    }

    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        try
        {
            var value = GetFlowAlgorithm(context).Arrange(
                finalSize,
                context,
                true,
                (FlowLayoutAlgorithm.LineAlignment)ItemsJustification,
                LayoutId);
            return new Size(value.Width, value.Height);
        }
        catch (COMException c)
        {
            return new Size(0, 0);
        }
        catch (Exception e)
        {
            return new Size(0, 0);
        }
    }


    protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object? source, NotifyCollectionChangedEventArgs args)
    {
        GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
        // Always invalidate layout to keep the view accurate.
        InvalidateLayout();

        var gridState = (UniformGridLayoutState)context.LayoutState!;
        gridState?.ClearElementOnDataSourceChange(context, args);
    }


    private double GetMinorSizeWithSpacing(VirtualizingLayoutContext context)
    {
        var minItemSpacing = MinColumnSpacing;
        var gridState = (UniformGridLayoutState)context.LayoutState!;
        return _orientation.ScrollOrientation == ScrollOrientation.Vertical ?
            gridState.EffectiveItemWidth + minItemSpacing :
            gridState.EffectiveItemHeight + minItemSpacing;
    }

    private double GetMajorSizeWithSpacing(VirtualizingLayoutContext context)
    {
        var lineSpacing = MinRowSpacing;
        var gridState = (UniformGridLayoutState)context.LayoutState!;
        return _orientation.ScrollOrientation == ScrollOrientation.Vertical ?
            gridState.EffectiveItemHeight + lineSpacing :
            gridState.EffectiveItemWidth + lineSpacing;
    }

    Rect GetLayoutRectForDataIndex(
        Size availableSize,
        int index,
        Rect lastExtent,
        VirtualizingLayoutContext context)
    {
        int itemsPerLine = (int)Math.Min( //note use of unsigned ints
            Math.Max(1u, (uint)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context))),
            Math.Max(1u, MaximumRowsOrColumnns));
        int rowIndex = (int)(index / itemsPerLine);
        int indexInRow = index - (rowIndex * itemsPerLine);

        var gridState = (UniformGridLayoutState)context.LayoutState!;
        Rect bounds = _orientation.MinorMajorRect(
            indexInRow * GetMinorSizeWithSpacing(context) + _orientation.MinorStart(lastExtent),
            rowIndex * GetMajorSizeWithSpacing(context) + _orientation.MajorStart(lastExtent),
            _orientation.ScrollOrientation == ScrollOrientation.Vertical ? gridState.EffectiveItemWidth : gridState.EffectiveItemHeight,
            _orientation.ScrollOrientation == ScrollOrientation.Vertical ? gridState.EffectiveItemHeight : gridState.EffectiveItemWidth);

        return bounds;
    }

    private void InvalidateLayout() => InvalidateMeasure();

    private static FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context) => ((UniformGridLayoutState)context.LayoutState!)?.FlowAlgorithm
    ?? new FlowLayoutAlgorithm();
}

/// <summary>
/// Represents the state of a <see cref="UniformGridLayout"/>.
/// </summary>
public class UniformGridLayoutState
{
    // We need to measure the element at index 0 to know what size to measure all other items. 
    // If FlowlayoutAlgorithm has already realized element 0 then we can use that. 
    // If it does not, then we need to do context.GetElement(0) at which point we have requested an element and are on point to clear it.
    // If we are responsible for clearing element 0 we keep m_cachedFirstElement valid. 
    // If we are not (because FlowLayoutAlgorithm is holding it for us) then we just null out this field and use the one from FlowLayoutAlgorithm.
    private UIElement? _cachedFirstElement;

    internal FlowLayoutAlgorithm FlowAlgorithm { get; } = new FlowLayoutAlgorithm();
    internal double EffectiveItemWidth { get; private set; }
    internal double EffectiveItemHeight { get; private set; }
    public int MeasureCount { get; set; }

    internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
    {
        FlowAlgorithm.InitializeForContext(context, callbacks);
        context.LayoutState = this;
    }

    internal void UninitializeForContext(VirtualizingLayoutContext context)
    {
        FlowAlgorithm.UninitializeForContext(context);

        if (_cachedFirstElement != null)
        {
            context.RecycleElement(_cachedFirstElement);
        }
    }

    internal void EnsureElementSize(
        Size availableSize,
        VirtualizingLayoutContext context,
        double layoutItemWidth,
        double layoutItemHeight,
        UniformGridLayoutItemsStretch stretch,
        Orientation orientation,
        double minRowSpacing,
        double minColumnSpacing,
        int maxItemsPerLine)
    {
        if (maxItemsPerLine == 0)
        {
            maxItemsPerLine = 1;
        }

        if (context.ItemCount > 0)
        {
            // If the first element is realized we don't need to cache it or to get it from the context
            var realizedElement = FlowAlgorithm.GetElementIfRealized(0);
            if (realizedElement != null)
            {
                realizedElement.Measure(availableSize);
                SetSize(realizedElement, layoutItemWidth, layoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);
                _cachedFirstElement = null;
            }
            else
            {
                if (_cachedFirstElement == null)
                {
                    // we only cache if we aren't realizing it
                    _cachedFirstElement = context.GetOrCreateElementAt(
                        0,
                        ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle); // expensive
                }

                _cachedFirstElement.Measure(availableSize);

                SetSize(_cachedFirstElement, layoutItemWidth, layoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing, maxItemsPerLine);

                // See if we can move ownership to the flow algorithm. If we can, we do not need a local cache.
                bool added = FlowAlgorithm.TryAddElement0(_cachedFirstElement);
                if (added)
                {
                    _cachedFirstElement = null;
                }
            }
        }
    }

    private void SetSize(
        UIElement element,
        double layoutItemWidth,
        double layoutItemHeight,
        Size availableSize,
        UniformGridLayoutItemsStretch stretch,
        Orientation orientation,
        double minRowSpacing,
        double minColumnSpacing,
        int maxItemsPerLine)
    {
        if (maxItemsPerLine == 0)
        {
            maxItemsPerLine = 1;
        }

        EffectiveItemWidth = (double.IsNaN(layoutItemWidth) ? element.DesiredSize.Width : layoutItemWidth);
        EffectiveItemHeight = (double.IsNaN(layoutItemHeight) ? element.DesiredSize.Height : layoutItemHeight);

        var availableSizeMinor = orientation == Orientation.Horizontal ? availableSize.Width : availableSize.Height;
        var minorItemSpacing = orientation == Orientation.Vertical ? minRowSpacing : minColumnSpacing;

        var itemSizeMinor = orientation == Orientation.Horizontal ? EffectiveItemWidth : EffectiveItemHeight;

        double extraMinorPixelsForEachItem = 0.0;
        if (!double.IsInfinity(availableSizeMinor))
        {
            var numItemsPerColumn = (int)Math.Min(
                maxItemsPerLine,
                Math.Max(1.0, availableSizeMinor / (itemSizeMinor + minorItemSpacing)));
            var usedSpace = (numItemsPerColumn * (itemSizeMinor + minorItemSpacing)) - minorItemSpacing;
            var remainingSpace = availableSizeMinor - usedSpace;
            extraMinorPixelsForEachItem = (int)(remainingSpace / numItemsPerColumn);
        }

        if (stretch == UniformGridLayoutItemsStretch.Fill)
        {
            if (orientation == Orientation.Horizontal)
            {
                EffectiveItemWidth += extraMinorPixelsForEachItem;
            }
            else
            {
                EffectiveItemHeight += extraMinorPixelsForEachItem;
            }
        }
        else if (stretch == UniformGridLayoutItemsStretch.Uniform)
        {
            var itemSizeMajor = orientation == Orientation.Horizontal ? EffectiveItemHeight : EffectiveItemWidth;
            var extraMajorPixelsForEachItem = itemSizeMajor * (extraMinorPixelsForEachItem / itemSizeMinor);
            if (orientation == Orientation.Horizontal)
            {
                EffectiveItemWidth += extraMinorPixelsForEachItem;
                EffectiveItemHeight += extraMajorPixelsForEachItem;
            }
            else
            {
                EffectiveItemHeight += extraMinorPixelsForEachItem;
                EffectiveItemWidth += extraMajorPixelsForEachItem;
            }
        }
    }

    internal void EnsureFirstElementOwnership(VirtualizingLayoutContext context)
    {
        if (_cachedFirstElement != null && FlowAlgorithm.GetElementIfRealized(0) != null)
        {
            // We created the element, but then flowlayout algorithm took ownership, so we can clear it and
            // let flowlayout algorithm do its thing.
            context.RecycleElement(_cachedFirstElement);
            _cachedFirstElement = null;
        }
    }



    internal void ClearElementOnDataSourceChange(
        VirtualizingLayoutContext context,
        NotifyCollectionChangedEventArgs args)
    {
        if (_cachedFirstElement != null)
        {
            bool shouldClear = false;
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    shouldClear = args.NewStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    shouldClear = args.NewStartingIndex == 0 || args.OldStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    shouldClear = args.OldStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Reset:
                    shouldClear = true;
                    break;

                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
            }

            if (shouldClear)
            {
                context.RecycleElement(_cachedFirstElement);
                _cachedFirstElement = null;
            }
        }
    }
}
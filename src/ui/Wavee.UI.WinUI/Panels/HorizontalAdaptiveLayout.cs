using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Panels;
internal sealed class HorizontalAdaptiveLayout : NonVirtualizingLayout
{
    public static readonly DependencyProperty DesiredWidthProperty = DependencyProperty.Register(nameof(DesiredWidth),
        typeof(double), typeof(HorizontalAdaptiveLayout),
        new PropertyMetadata(200, DesiredWidthChanged));
    public double DesiredWidth
    {
        get => (double)GetValue(DesiredWidthProperty);
        set => SetValue(DesiredWidthProperty, value);
    }
    private static void DesiredWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (HorizontalAdaptiveLayout)d;
        panel.InvalidateMeasure();
    }

    protected override void InitializeForContextCore(NonVirtualizingLayoutContext context)
    {
        base.InitializeForContextCore(context);

        var state = context.LayoutState as ActivityFeedLayoutState;
        if (state == null)
        {
            // Store any state we might need since (in theory) the layout could be in use by multiple
            // elements simultaneously
            // In reality for the Xbox Activity Feed there's probably only a single instance.
            context.LayoutState = new ActivityFeedLayoutState();
        }
    }
    protected override void UninitializeForContextCore(NonVirtualizingLayoutContext context)
    {
        base.UninitializeForContextCore(context);

        // clear any state
        context.LayoutState = null;
    }


    protected override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
    {
        // Determine which items will appear on those rows and what the rect will be for each item
        var state = context.LayoutState as ActivityFeedLayoutState;
        state.LayoutRects.Clear();

        var items = context.Children.Count;
        if (items == 0)
            return new Size(0, 0);

        /*
      * Example:
                
                Available width = 1000
                We can fit 1000/200 = 5 items without resizing, do that
                
                Available width = 932 
                we can fit 932/200 = 4.66 items, rounding up means 5
                So we have 0.34 less items, that means each item should get resized DOWN so we have enough width to fit 0.34 items
      */
        var availableWidth = availableSize.Width;
        var fitItems = (int)Math.Floor(availableWidth / DesiredWidth);
        var resize =
            availableWidth - (fitItems * DesiredWidth);
        var resizePerItem =
            fitItems > items ? 0 : resize / fitItems;

        double totalWidth = 0;
        double totalHeight = 0;
        var count = Math.Min(fitItems, items);
        double maxHeight = 0;

        for (var i = 0; i < count; i++)
        {
            const int UPPER_LIMIT = 10;
            for (var j = 0; j < UPPER_LIMIT; j++)
            {
                try
                {
                    var item = context.Children[i];
                    if (item is null)
                        break;
                    var additionalWidth = DesiredWidth + resizePerItem;

                    item.Measure(new Size(additionalWidth, double.PositiveInfinity));
                    var additionalHeight = item.DesiredSize.Height;
                    state.LayoutRects.Add(new Rect(totalWidth, 0, additionalWidth, additionalHeight));
                    totalWidth += additionalWidth;
                    totalHeight = Math.Max(additionalHeight, totalHeight);
                    maxHeight = Math.Max(maxHeight, additionalHeight);
                    break;
                }
                catch (COMException e)
                {

                }
            }
        }

        return new Size(totalWidth, totalHeight);
    }
    protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
    {
        // walk through the cache of containers and arrange
        var state = context.LayoutState as ActivityFeedLayoutState;
        var virtualContext = context as NonVirtualizingLayoutContext;
        int currentIndex = state.FirstRealizedIndex;

        foreach (var arrangeRect in state.LayoutRects)
        {
            const int UPPER_LIMIT = 10;
            for (var j = 0; j < UPPER_LIMIT; j++)
            {
                try
                {
                    var container = virtualContext.Children.ElementAt(currentIndex);
                    container.Arrange(arrangeRect);
                    currentIndex++;
                    break;
                }
                catch (COMException)
                {

                }
            }
        }

        return finalSize;
    }
    internal class ActivityFeedLayoutState
    {
        public int FirstRealizedIndex { get; set; }

        /// <summary>
        /// List of layout bounds for items starting with the
        /// FirstRealizedIndex.
        /// </summary>
        public List<Rect> LayoutRects
        {
            get
            {
                if (_layoutRects == null)
                {
                    _layoutRects = new List<Rect>();
                }

                return _layoutRects;
            }
        }

        private List<Rect> _layoutRects;
    }
}
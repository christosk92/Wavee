using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using Eum.UI.ViewModels.Search;
using Eum.UI.ViewModels.Search.Sources;

namespace Eum.UI.WinUI.Views.Search
{
    public class SearchFeedLayout : VirtualizingLayout
    {// STEP #2 - Parameterize the layout
        #region Layout parameters

        // We'll cache copies of the dependency properties to avoid calling GetValue during layout since that
        // can be quite expensive due to the number of times we'd end up calling these.
        private double _rowSpacing;
        private double _colSpacing;
        private Size _minItemSize = Size.Empty;

        /// <summary>
        /// Gets or sets the size of the whitespace gutter to include between rows
        /// </summary>
        public double RowSpacing
        {
            get { return _rowSpacing; }
            set { SetValue(RowSpacingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the size of the whitespace gutter to include between items on the same row
        /// </summary>
        public double ColumnSpacing
        {
            get { return _colSpacing; }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        public Size MinItemSize
        {
            get { return _minItemSize; }
            set { SetValue(MinItemSizeProperty, value); }
        }

        public static readonly DependencyProperty RowSpacingProperty =
            DependencyProperty.Register(
                nameof(RowSpacing),
                typeof(double),
                typeof(SearchFeedLayout),
                new PropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty ColumnSpacingProperty =
            DependencyProperty.Register(
                nameof(ColumnSpacing),
                typeof(double),
                typeof(SearchFeedLayout),
                new PropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty MinItemSizeProperty =
            DependencyProperty.Register(
                nameof(MinItemSize),
                typeof(Size),
                typeof(SearchFeedLayout),
                new PropertyMetadata(Size.Empty, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var layout = obj as SearchFeedLayout;
            if (args.Property == RowSpacingProperty)
            {
                layout._rowSpacing = (double)args.NewValue;
            }
            else if (args.Property == ColumnSpacingProperty)
            {
                layout._colSpacing = (double)args.NewValue;
            }
            else if (args.Property == MinItemSizeProperty)
            {
                layout._minItemSize = (Size)args.NewValue;
            }
            else
            {
                throw new InvalidOperationException("Don't know what you are talking about!");
            }

            layout.InvalidateMeasure();
        }

        #endregion


        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            base.InitializeForContextCore(context);

            var state = context.LayoutState as SearchFeedLayoutState;
            if (state == null)
            {
                // Store any state we might need since (in theory) the layout could be in use by multiple
                // elements simultaneously
                // In reality for the Xbox Activity Feed there's probably only a single instance.
                context.LayoutState = new SearchFeedLayoutState();
            }
        }
        protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            base.UninitializeForContextCore(context);

            // clear any state
            context.LayoutState = null;
        }
        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            //Check if the first item 
            try
            {
                if (this.MinItemSize == Size.Empty)
                {
                    var firstElement = context.GetOrCreateElementAt(0);
                    firstElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    // setting the member value directly to skip invalidating layout
                    this._minItemSize = firstElement.DesiredSize;
                }

                bool didHaveTopResult = false;
                bool didStretchRowAfterTopResult = false;

                // Determine which rows need to be realized.  We know every row will have the same height and
                //  contain at most 2 items.  Use that to determine the index for the first and last item that
                // will be within that realization rect.

                var firstRowIndex = Math.Max(
                    (int)(context.RealizationRect.Y / (this.MinItemSize.Height + this.RowSpacing)) - 1,
                    0);

                // Determine which items will appear on those rows and what the rect will be for each item
                var state = context.LayoutState as SearchFeedLayoutState;
                state.LayoutRects.Clear();

                state.FirstRealizedIndex = firstRowIndex * 2;


                // ideal item width that will expand/shrink to fill available space
                double desiredItemWidth = Math.Max(this.MinItemSize.Width,
                    (availableSize.Width - this.ColumnSpacing * 2) / 4);

                Rect topResultRect = default;
                var elements = new List<FrameworkElement>();
                for (int i = 0; i < context.ItemCount; i++)
                {
                    var element = context.GetOrCreateElementAt(i);
                    if (element is FrameworkElement f2)
                    {
                        elements.Add(f2);
                    }
                }

                elements = elements
                    .OrderBy(a => (a.DataContext as SearchItemGroup).Order)
                    .ToList();
                var dtx = elements.Select(a => a.DataContext as SearchItemGroup);

                int row = 0;
                foreach (var f in elements)
                {
                    switch (f.DataContext)
                    {
                        //should be i = 0;
                        case TopResultGroup:
                            {
                                //if it's a top result, use the provided width
                                didHaveTopResult = true;
                                topResultRect = CalculateRect(HorizontalAlignment.Left,
                                    Rect.Empty, 0, Math.Min(desiredItemWidth, f.MaxWidth), f.Height);
                                f.Measure(
                                    new Size(topResultRect.Width, topResultRect.Height));

                                state.LayoutRects.Add(topResultRect);

                                break;
                            }
                        case RecommendationsResultGroup:
                            didStretchRowAfterTopResult = true;
                            var recommendationsResultRect = CalculateRect(HorizontalAlignment.Stretch,
                                topResultRect, 0, context.RealizationRect.Width, f.Height);
                            f.Measure(
                                new Size(recommendationsResultRect.Width, recommendationsResultRect.Height));
                            state.LayoutRects.Add(recommendationsResultRect);
                            row++;
                            break;
                        case SongsResultGroup:
                            if (didHaveTopResult)
                            {
                                if (!didStretchRowAfterTopResult)
                                {
                                    //set it next to topResult
                                    var tracksRect = CalculateRect(HorizontalAlignment.Stretch,
                                        topResultRect, 0, context.RealizationRect.Width, f.Height);
                                    f.Measure(
                                        new Size(tracksRect.Width, tracksRect.Height));
                                    state.LayoutRects.Add(tracksRect);
                                    row++;
                                }
                                else
                                {
                                    //already filled.. ignore
                                }
                            }
                            else
                            {
                                //did not have a top result..  Set as regular stack
                                var regularRect2 = CalculateRect(HorizontalAlignment.Stretch,
                                    default, row, context.RealizationRect.Width, f.Height);
                                f.Measure(
                                    new Size(regularRect2.Width, regularRect2.Height));
                                state.LayoutRects.Add(regularRect2);
                                row++;
                            }
                            break;
                        default:
                            var regularRect = CalculateRect(HorizontalAlignment.Stretch,
                                default, row, context.RealizationRect.Width, f.Height);
                            f.Measure(
                                new Size(regularRect.Width, regularRect.Height));
                            state.LayoutRects.Add(regularRect);
                            row++;
                            break;
                    }
                }
                var lastRowIndex = Math.Min(
                (int)(context.RealizationRect.Bottom / (this.MinItemSize.Height + this.RowSpacing)) + 1,
                (int)(context.ItemCount / 3));


                //
                // for (int rowIndex = firstRowIndex; rowIndex < lastRowIndex; rowIndex++)
                // {
                //     int firstItemIndex = rowIndex * 2;
                //     var boundsForCurrentRow = CalculateLayoutBoundsForRow(rowIndex, desiredItemWidth);
                //
                //     for (int columnIndex = 0; columnIndex < 2; columnIndex++)
                //     {
                //         var index = firstItemIndex + columnIndex;
                //         var rect = boundsForCurrentRow[index % 2];
                //         var container = context.GetOrCreateElementAt(index);
                //
                //         container.Measure(
                //             new Size(boundsForCurrentRow[columnIndex].Width, boundsForCurrentRow[columnIndex].Height));
                //
                //         state.LayoutRects.Add(boundsForCurrentRow[columnIndex]);
                //     }
                // }

                // Calculate and return the size of all the content (realized or not) by figuring out
                // what the bottom/right position of the last item would be.
                var extentHeight = ((int)(context.ItemCount / 2) - 1) * (this.MinItemSize.Height + this.RowSpacing) +
                                   this.MinItemSize.Height;

                // Report this as the desired size for the layout
                return new Size(desiredItemWidth * 4 + this.ColumnSpacing * 2, extentHeight);
            }
            catch (Exception x)
            {
                return new Size(0, 0);
            }
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            // walk through the cache of containers and arrange
            var state = context.LayoutState as SearchFeedLayoutState;
            var virtualContext = context as VirtualizingLayoutContext;
            int currentIndex = state.FirstRealizedIndex;

            foreach (var arrangeRect in state.LayoutRects)
            {
                var container = virtualContext.GetOrCreateElementAt(currentIndex);
                if (arrangeRect.Width < 100000000)
                {
                    container.Arrange(arrangeRect);
                }
                currentIndex++;
            }

            return finalSize;
        }

        private Rect CalculateRect(
            HorizontalAlignment horizontalAlignment,
            Rect existingColumnRect,
            int rowIndex,
            double desiredItemWidth,
            double height)
        {
            var r = new Rect();
            var yoffset = rowIndex * (this.MinItemSize.Height + this.RowSpacing);
            if (horizontalAlignment == HorizontalAlignment.Stretch)
            {
                //fill full row
                r.X = existingColumnRect.Right + this.ColumnSpacing;
                r.Y = yoffset;
                r.Height = height;
                r.Width = desiredItemWidth - existingColumnRect.Width;
                return r;
            }

            r.X = 0;
            r.Y = yoffset;
            r.Height = height;
            r.Width = desiredItemWidth;
            return r;
            // var boundsForRow = new Rect[3];
            //
            // var yoffset = rowIndex * (this.MinItemSize.Height + this.RowSpacing);
            // boundsForRow[0].Y = boundsForRow[1].Y = boundsForRow[2].Y = yoffset;
            // boundsForRow[0].Height = boundsForRow[1].Height = boundsForRow[2].Height = this.MinItemSize.Height;
            //
            // if (rowIndex % 2 == 0)
            // {
            //     // Left tile (narrow)
            //     boundsForRow[0].X = 0;
            //     boundsForRow[0].Width = desiredItemWidth;
            //     // Middle tile (narrow)
            //     boundsForRow[1].X = boundsForRow[0].Right + this.ColumnSpacing;
            //     boundsForRow[1].Width = desiredItemWidth;
            //     // Right tile (wide)
            //     boundsForRow[2].X = boundsForRow[1].Right + this.ColumnSpacing;
            //     boundsForRow[2].Width = desiredItemWidth * 2 + this.ColumnSpacing;
            // }
            // else
            // {
            //     // Left tile (wide)
            //     boundsForRow[0].X = 0;
            //     boundsForRow[0].Width = (desiredItemWidth * 2 + this.ColumnSpacing);
            //     // Middle tile (narrow)
            //     boundsForRow[1].X = boundsForRow[0].Right + this.ColumnSpacing;
            //     boundsForRow[1].Width = desiredItemWidth;
            //     // Right tile (narrow)
            //     boundsForRow[2].X = boundsForRow[1].Right + this.ColumnSpacing;
            //     boundsForRow[2].Width = desiredItemWidth;
            // }
            //
            // return boundsForRow;
        }

    }

    internal class SearchFeedLayoutState
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

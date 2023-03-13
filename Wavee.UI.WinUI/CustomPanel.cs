 using System.Linq;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace Wavee.UI.WinUI;

public class CustomPanel : Panel
{
    public static readonly DependencyProperty ColumnsRatioProperty = DependencyProperty.Register(nameof(ColumnsRatio),
        typeof(AspectRatio), typeof(CustomPanel), new PropertyMetadata(default(AspectRatio), PropertyChangedCallback));

    public static readonly DependencyProperty RowsRatioProperty = DependencyProperty.Register(nameof(RowsRatio),
        typeof(AspectRatio),
        typeof(CustomPanel),
        new PropertyMetadata(default(AspectRatio), PropertyChangedCallback));

    public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register(nameof(HeaderHeight),
        typeof(double), typeof(CustomPanel), new PropertyMetadata(default(double), PropertyChangedCallback));

    public static readonly DependencyProperty MinAvailableWidthProperty =
        DependencyProperty.Register(nameof(MinAvailableWidth), typeof(double), typeof(CustomPanel),
            new PropertyMetadata(default(double)));

    protected override Size MeasureOverride(Size availableSize)
    {
        double totalWidth = 0;
        double totalHeight = 0;

        for (int i = 0; i < Children.Count; i += 3)
        {
            // Measure the first item
            var firstItem = Children[i];
            firstItem.Measure(availableSize);

            if (availableSize.Width < MinAvailableWidth)
            {
                // Measure the second and third items
                var remainingWidth = availableSize.Width;
                //availableSize.Height is infinity
                //lets say we have 4 items
                //we will be on 3rd index
                //4 > 3 + 1 + 2 ? 
                //4 > 5 (NO)
                //but if we have 6 items
                //6 > 3 + 1 +2 ? 
                // 6 > 6 NO, but we need it, so -1
                if (Children.Count > i + 2)
                {
                    var secondItem = Children[i + 1];
                    var secondItemHeight = HeaderHeight * RowsRatio.Width / (RowsRatio.Width + RowsRatio.Height);
                    secondItem.Measure(new Size(remainingWidth, secondItemHeight));

                    var thirdItem = Children[i + 2];
                    var thirdItemHeight = HeaderHeight * RowsRatio.Height / (RowsRatio.Width + RowsRatio.Height);
                    thirdItem.Measure(new Size(remainingWidth, thirdItemHeight));

                    // Add up the total desired size of all groups
                    totalHeight += HeaderHeight + secondItemHeight + thirdItemHeight;
                }
                else
                {
                    totalHeight += HeaderHeight;
                }
                totalWidth = Math.Max(totalWidth, firstItem.DesiredSize.Width);

            }
            else
            {
                // Measure the second and third items
                var remainingWidth = availableSize.Width - firstItem.DesiredSize.Width;
                var remainingHeight = HeaderHeight;
                if (Children.Count > i + 1)
                {
                    var secondItem = Children[i + 1];
                    var secondHeight = remainingHeight * RowsRatio.Width / (RowsRatio.Width + RowsRatio.Height);
                    secondItem.Measure(new Size(remainingWidth,
                        secondHeight));
                    totalWidth = Math.Max(totalWidth,
                        firstItem.DesiredSize.Width);
                    totalHeight += secondHeight;
                }

                if (Children.Count > i + 2)
                {
                    var thirdItem = Children[i + 2];
                    var thirdHeight = remainingHeight * RowsRatio.Height / (RowsRatio.Width + RowsRatio.Height);
                    thirdItem.Measure(new Size(remainingWidth,
                        thirdHeight));

                    // Add up the total desired size of all groups
                    totalWidth = Math.Max(totalWidth,
                        firstItem.DesiredSize.Width + Children[i + 1].DesiredSize.Width + thirdItem.DesiredSize.Width);
                    totalHeight += thirdHeight;
                }
                else
                {
                    totalWidth = Math.Max(totalWidth,
                        firstItem.DesiredSize.Width);
                    totalHeight += HeaderHeight;
                }
            }
        }

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;

        for (int i = 0; i < Children.Count; i += 3)
        {
            var availableWidth = finalSize.Width;
            var firstItem = Children[i];
            double firstItemWidth;
            double secondItemWidth;
            double secondItemHeight;
            double thirdItemHeight;

            UIElement secondItem;
            UIElement thirdItem;
            if (availableWidth < MinAvailableWidth)
            {
                // Stack all items and fill the available height
                firstItemWidth = availableWidth;
                firstItem.Arrange(new Rect(0, y, firstItemWidth, HeaderHeight));

                if (Children.Count > i + 1)
                {
                    secondItemHeight = HeaderHeight * RowsRatio.Width / (RowsRatio.Width + RowsRatio.Height);

                    secondItem = Children[i + 1];
                    secondItemWidth = availableWidth;
                    secondItem.Arrange(new Rect(0, y + HeaderHeight, secondItemWidth, secondItemHeight));
                }
                else
                {
                    secondItemHeight = 0;
                    secondItemWidth = 0;
                }

                if (Children.Count > i + 2)
                {
                    thirdItem = Children[i + 2];
                    thirdItemHeight = HeaderHeight * RowsRatio.Height / (RowsRatio.Width + RowsRatio.Height);
                    thirdItem.Arrange(
                        new Rect(0, y + HeaderHeight + secondItemHeight, secondItemWidth, thirdItemHeight));
                }
                else
                {
                    thirdItemHeight = 0;
                }

                y += HeaderHeight + secondItemHeight + thirdItemHeight;
            }
            else
            {
                // Arrange the items side by side
                //the width is defined by the ColumnsRatio, it should fill the available width in a ratio defined by ColumnsRatio
                if (Children.Count > i + 1)
                {
                    firstItemWidth = finalSize.Width * (ColumnsRatio.Width) /
                                     (ColumnsRatio.Width + ColumnsRatio.Height);
                    firstItem.Arrange(new Rect(0, y, firstItemWidth, HeaderHeight));

                    secondItem = Children[i + 1];
                    //the height of the SECOND item is defined by the RowsRatio, it should fill the height of the first item in a ratio defined by RowsRatio
                    //the width should fill the remaining space in a ratio defined by ColumnsRatio
                    secondItemHeight = HeaderHeight * (RowsRatio.Width) / (RowsRatio.Height + RowsRatio.Width);
                    secondItemWidth = finalSize.Width - firstItemWidth;
                    secondItem.Arrange(new Rect(firstItemWidth, y, secondItemWidth, secondItemHeight));
                }
                else
                {
                    firstItemWidth = finalSize.Width;
                    firstItem.Arrange(new Rect(0, y, firstItemWidth, HeaderHeight));

                    secondItemHeight = 0;
                    secondItemWidth = 0;
                }

                if (Children.Count > i + 2)
                {

                    thirdItem = Children[i + 2];
                    //the height of the THIRD item is defined by the RowsRatio, it should fill the remaining height in a ratio defined by RowsRatio
                    //the width should fill the remaining space in a ratio defined by ColumnsRatio
                    //   thirdItemHeight = availableHeight - HeaderHeight - secondItemHeight - y;
                    thirdItemHeight = HeaderHeight * (RowsRatio.Height / (RowsRatio.Height + RowsRatio.Width));
                    thirdItem.Arrange(new Rect(firstItemWidth, y + secondItemHeight, secondItemWidth, thirdItemHeight));
                }

                // Update the y position for the next group of items
                y += HeaderHeight;
            }
        }

        return finalSize;
    }


    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var layout = d as CustomPanel;
        layout.InvalidateMeasure();
    }

    // protected override Size ArrangeOverride(Size finalSize)
    // {
    //     
    // }
    //
    // protected override Size MeasureOverride(Size availableSize)
    // {
    //   
    // }

    public AspectRatio ColumnsRatio
    {
        get => (AspectRatio)GetValue(ColumnsRatioProperty);
        set => SetValue(ColumnsRatioProperty, value);
    }

    public AspectRatio RowsRatio
    {
        get => (AspectRatio)GetValue(RowsRatioProperty);
        set => SetValue(RowsRatioProperty, value);
    }

    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public double MinAvailableWidth
    {
        get => (double)GetValue(MinAvailableWidthProperty);
        set => SetValue(MinAvailableWidthProperty, value);
    }
}
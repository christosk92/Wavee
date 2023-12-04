using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Layout
{
    internal sealed class HorizontalPanelLayout : Panel
    {
        public static readonly DependencyProperty DesiredWidthProperty = DependencyProperty.Register(nameof(DesiredWidth),
            typeof(double), typeof(HorizontalPanelLayout),
            new PropertyMetadata(200, DesiredWidthChanged));

        private static void DesiredWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (HorizontalPanelLayout)d;
            panel.InvalidateMeasure();
        }

        public HorizontalPanelLayout()
        {

        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Children.Count;
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

            // if (double.IsPositiveInfinity(availableSize.Width))
            // {
            //     availableSize.Width = LvBase.ActualWidth;
            // }
            var availableWidth = availableSize.Width;
            var fitItems = (int)Math.Floor(availableWidth / DesiredWidth);
            var resize =
                availableWidth - (fitItems * DesiredWidth);
            var resizePerItem =
                fitItems > items ? 0 : resize / fitItems;

            double totalWidth = 0;
            double totalHeight = 0;
            var count = Math.Min(fitItems, items);
            //measure items
            for (var i = 0; i < count; i++)
            {
                var item = Children[i];
                var additionalWidth = DesiredWidth + resizePerItem;

                item.Measure(new Size(additionalWidth, double.PositiveInfinity));
                var additionalHeight = item.DesiredSize.Height;
                totalWidth += additionalWidth;
                totalHeight = Math.Max(additionalHeight, totalHeight);
            }

            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //horizontal fit layout 
            var items = Children.Count;
            if (items == 0)
                return new Size(0, 0);

            var availableWidth = finalSize.Width;
            var fitItems = (int)Math.Floor(availableWidth / DesiredWidth);
            var calculatedFitItems = availableWidth / DesiredWidth;
            double x = 0;
            double y = 0;
            double maxHeight = 0;
            for (var i = 0; i < items; i++)
            {
                var item = Children[i];
                item.Arrange(new Rect(x, y, item.DesiredSize.Width, item.DesiredSize.Height));
                x += (item.DesiredSize.Width);
                maxHeight = Math.Max(maxHeight, item.DesiredSize.Height);
            }

            // if (finalSize.Height == 0)
            // {
            //     this.InvalidateMeasure();
            // }
            return new Size(finalSize.Width, Math.Max(maxHeight, finalSize.Height));
        }

        public double DesiredWidth
        {
            get => (double)GetValue(DesiredWidthProperty);
            set => SetValue(DesiredWidthProperty, value);
        }
    }
}
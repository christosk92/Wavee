using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Core.Contracts.Search;
using static Wavee.UI.WinUI.Panels.HorizontalAdaptiveLayout;
using Wavee.UI.ViewModel.Search;

namespace Wavee.UI.WinUI.Panels;

public sealed class SearchResultsPanel : NonVirtualizingLayout
{
    public static readonly DependencyProperty DesiredWidthProperty = DependencyProperty.Register(nameof(DesiredWidth), typeof(double), typeof(SearchResultsPanel), new PropertyMetadata(default(double)));

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
        //the context.Children is a list of UIElements that are the children of the panel
        //the datacontext (and the next item after the current item), determines the layout of the panel

        //For example, if we have a Highlight + tracks, we want to measure the highlight, then the tracks
        //the highlight will be able to size itself, but the tracks will need to be sized to the available space

        //After that, every other item arranges itself in a vertical stack layout

        var state = context.LayoutState as ActivityFeedLayoutState;
        state.LayoutRects.Clear();

        var items = context.Children.Count;
        if (items == 0)
            return new Size(0, 0);

        var availableWidth = availableSize.Width;
        var children = context.Children;
        bool previouswasHighlight = false;
        int addedItems = 0;
        double addedHeight = 0;
        double addedWidth = 0;
        for (var i = 0; i < children.Count; i++)
        {
            //check the item type (by checking the datacontext)
            //if it's a highlight, measure it
            //if it's a track, measure it with the available width
            //if its recommended, measure it with the available width
            var child = children[i];
            var datacontext = ((FrameworkElement)child).Tag;
            if (datacontext is GroupedSearchResult search)
            {
                switch (search.Group)
                {
                    case SearchGroup.Highlighted:
                        previouswasHighlight = true;
                        //highlight can resize itself (but for now assume a 300x150 size)
                        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                        var highlightSize = child.DesiredSize;
                        state.LayoutRects.Add(new Rect(0, 0, highlightSize.Width, highlightSize.Height));
                        child.Measure(highlightSize);
                        addedItems++;
                        addedHeight += highlightSize.Height;
                        addedWidth += highlightSize.Width;
                        break;
                    case SearchGroup.Recommended:
                    case SearchGroup.Track:
                        {
                            if (previouswasHighlight)
                            {
                                //measure with the available width
                                var trackSize = new Size(availableWidth - addedWidth, addedHeight);
                                state.LayoutRects.Add(new Rect(addedWidth, 0, trackSize.Width, trackSize.Height));
                                child.Measure(trackSize);
                            }
                            else
                            {
                                var added = SizeLikeHorizontalAdaptiveLayout(child, availableWidth, state.LayoutRects, addedHeight);
                                addedHeight += added;
                            }

                            previouswasHighlight = false;
                            break;
                        }
                    default:
                        {
                            previouswasHighlight = false;
                            var added = SizeLikeHorizontalAdaptiveLayout(child, availableWidth, state.LayoutRects, addedHeight);
                            addedHeight += added;
                            break;
                        }

                }
            }
        }

        return new Size(availableWidth, addedHeight);
        //return base.MeasureOverride(context, availableSize);
    }

    private double SizeLikeHorizontalAdaptiveLayout(UIElement child, double availableWidth, List<Rect> stateLayoutRects, double addedHeight)
    {
        var infinite = new Size(availableWidth, double.PositiveInfinity);
        child.Measure(infinite);
        var childHeight = child.DesiredSize.Height;

        var childRect = new Rect(0, addedHeight, availableWidth, childHeight);
        stateLayoutRects.Add(childRect);
        return childHeight;
    }

    protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
    {

        var state = context.LayoutState as ActivityFeedLayoutState;
        var virtualContext = context as NonVirtualizingLayoutContext;
        foreach (var arrangeRecht in state.LayoutRects)
        {
            var child = virtualContext.Children[state.LayoutRects.IndexOf(arrangeRecht)];
            child.Arrange(arrangeRecht);
        }

        return finalSize;
        return base.ArrangeOverride(context, finalSize);
    }

    public double DesiredWidth
    {
        get => (double)GetValue(DesiredWidthProperty);
        set => SetValue(DesiredWidthProperty, value);
    }
}
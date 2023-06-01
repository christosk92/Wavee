using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Flyouts;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;
using Microsoft.UI.Xaml.Media;
using Canvas = Microsoft.UI.Xaml.Controls.Canvas;
using Microsoft.UI.Xaml.Hosting;
using CommunityToolkit.WinUI.UI.Animations.Expressions;
using Eum.Spotify.context;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Playback;
using Button = Microsoft.UI.Xaml.Controls.Button;
using HashMap = LanguageExt.HashMap;

namespace Wavee.UI.WinUI.Views.Album;

public sealed partial class AlbumView : UserControl, INavigablePage
{
    CompositionPropertySet _props;
    CompositionPropertySet _scrollerPropertySet;
    Compositor _compositor;
    public AlbumView()
    {
        ViewModel = new AlbumViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public bool ShouldKeepInCache(int _)
    {
        //never keep in cache
        return false;
    }

    Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;
    public void NavigatedTo(object parameter)
    {

    }

    public void RemovedFromCache()
    {
        ViewModel.Clear();
        _props.Dispose();
        _scrollerPropertySet.Dispose();
        _compositor = null;
    }

    public AlbumViewModel<WaveeUIRuntime> ViewModel { get; }

    private async void AlbumView_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.AlbumFetched.Task;
        UpdateBindings();
        ElementCompositionPreview.SetIsTranslationEnabled(MainImage, true);
        ElementCompositionPreview.SetIsTranslationEnabled(MainHeader, true);
        ElementCompositionPreview.SetIsTranslationEnabled(ButtonsPanel, true);
        ElementCompositionPreview.SetIsTranslationEnabled(AlbumTitle, true);

        //MainScroller
        var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)MainHeader);
        Canvas.SetZIndex((UIElement)MainHeader, 1);
        Canvas.SetZIndex(MainScroller, 2);
        // Get the PropertySet that contains the scroll values from the ScrollViewer
        _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(MainScroller);
        _compositor = _scrollerPropertySet.Compositor;

        // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar("progress", 0);
        _props.InsertScalar("clampSize", 100);
        _props.InsertScalar("scaleFactor", 0.6f);

        // Get references to our property sets for use with ExpressionNodes
        var scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
        var props = _props.GetReference();
        var progressNode = props.GetScalarProperty("progress");
        var clampSizeNode = props.GetScalarProperty("clampSize");
        var scaleFactorNode = props.GetScalarProperty("scaleFactor");

        // Create and start an ExpressionAnimation to track scroll progress over the desired distance
        ExpressionNode progressAnimation = ExpressionFunctions.Clamp(-scrollingProperties.Translation.Y / clampSizeNode, 0, 1);
        _props.StartAnimation("progress", progressAnimation);

        // Get the backing visual for the header so that its properties can be animated
        Visual headerVisual = ElementCompositionPreview.GetElementVisual(MainHeader);


        // Create and start an ExpressionAnimation to clamp the header's offset to keep it onscreen
        ExpressionNode headerTranslationAnimation = ExpressionFunctions.Conditional(progressNode < 1, 0, -scrollingProperties.Translation.Y - clampSizeNode);
        headerVisual.StartAnimation("Translation.Y", headerTranslationAnimation);

        // Create and start an ExpressionAnimation to scale the header during overpan
        ExpressionNode headerScaleAnimation = ExpressionFunctions.Lerp(1, 1.25f,
            ExpressionFunctions.Clamp(scrollingProperties.Translation.Y / 50, 0, 1));
        headerVisual.StartAnimation("Scale.X", headerScaleAnimation);
        headerVisual.StartAnimation("Scale.Y", headerScaleAnimation);

        //Set the header's CenterPoint to ensure the overpan scale looks as desired
        headerVisual.CenterPoint = new Vector3((float)(MainHeader.ActualWidth / 2), (float)MainHeader.ActualHeight, 0);

        // Get the backing visual for the profile picture visual so that its properties can be animated
        Visual profileVisual = ElementCompositionPreview.GetElementVisual(MainImage);
        // Create and start an ExpressionAnimation to scale the profile image with scroll position
        ExpressionNode scaleAnimation = ExpressionFunctions.Lerp(1, scaleFactorNode, progressNode);
        ExpressionNode profileOffsetAnimation = progressNode * 100;

        profileVisual.StartAnimation("Scale.X", scaleAnimation);
        profileVisual.StartAnimation("Scale.Y", scaleAnimation);
        profileVisual.StartAnimation("Translation.Y", profileOffsetAnimation);
        // Get backing visuals for the text blocks so that their properties can be animated
        Visual blurbVisual = ElementCompositionPreview.GetElementVisual(AlbumType);
        Visual subtitleVisual = ElementCompositionPreview.GetElementVisual(Artists);
        Visual moreVisual = ElementCompositionPreview.GetElementVisual(Metadata);

        // Create an ExpressionAnimation that moves between 1 and 0 with scroll progress, to be used for text block opacity
        ExpressionNode textOpacityAnimation = ExpressionFunctions.Clamp(1 - (progressNode * 2), 0, 1);

        // Start opacity and scale animations on the text block visuals
        blurbVisual.StartAnimation("Opacity", textOpacityAnimation);
        blurbVisual.StartAnimation("Scale.X", scaleAnimation);
        blurbVisual.StartAnimation("Scale.Y", scaleAnimation);

        subtitleVisual.StartAnimation("Opacity", textOpacityAnimation);
        subtitleVisual.StartAnimation("Scale.X", scaleAnimation);
        subtitleVisual.StartAnimation("Scale.Y", scaleAnimation);

        moreVisual.StartAnimation("Opacity", textOpacityAnimation);
        moreVisual.StartAnimation("Scale.X", scaleAnimation);
        moreVisual.StartAnimation("Scale.Y", scaleAnimation);

        // Get the backing visuals for the text and button containers so that their properites can be animated
        Visual textVisual = ElementCompositionPreview.GetElementVisual(AlbumTitle);
        Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonsPanel);

        // When the header stops scrolling it is 150 pixels offscreen.  We want the text header to end up with 50 pixels of its content
        // offscreen which means it needs to go from offset 0 to 100 as we traverse through the scrollable region
        ExpressionNode contentOffsetAnimation = progressNode * 100;
        textVisual.StartAnimation("Translation.Y", contentOffsetAnimation);
        textVisual.StartAnimation("Translation.X", progressNode * -100);

        ExpressionNode buttonOffsetAnimation = progressNode * 0;
        buttonVisual.StartAnimation("Translation.Y", buttonOffsetAnimation);
        buttonVisual.StartAnimation("Translation.X", progressNode * -100);
    }

    private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        UpdateBindings();
    }

    private void UpdateBindings()
    {
        this.Bindings.Update();
        MainImage.Source = new BitmapImage(new Uri(ViewModel.Image));
        TotalDuration.Text = TotalSum().ToString(@"mm\:ss");
        AlbumType.Text = ViewModel.Type.ToUpper();
        if (ViewModel.Month.IsSome)
        {
            var month = ViewModel.Month.ValueUnsafe();
            var dateOnly = new DateOnly(
                year: ViewModel.Year,
                month: month,
                day: 1
            );

            string fullMonthName =
                dateOnly.ToString("MMMM");

            if (ViewModel.Day.IsSome)
            {
                var day = ViewModel.Day.ValueUnsafe();
                MoreDescription.Text = $"{fullMonthName} {day}, {dateOnly.Year}";
            }
            else
            {
                MoreDescription.Text = $"{fullMonthName}, {dateOnly.Year}";
            }
        }
        else
        {
            MoreDescription.Text = ViewModel.Year.ToString(CultureInfo.InvariantCulture);
        }
    }
    private TimeSpan TotalSum()
    {
        var totalSum = ViewModel.Discs
            .Sum(x => x.Tracks.Sum(f => f.Duration.TotalMilliseconds));
        return TimeSpan.FromMilliseconds(totalSum);
    }

    private void RelatedAlbumTapped(object sender, TappedRoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag;
        if (tag is not AudioId id)
        {
            return;
        }

        //if the originalSource contains ButtonsPanel, we tapped on a button and we don't want to navigate
        if (e.OriginalSource is FrameworkElement originalSource
            && originalSource.FindAscendantOrSelf<StackPanel>(x => x.Name is "ButtonsPanel") is { })
        {
            return;
        }

        UICommands.NavigateTo.Execute(id);
    }

    /*
     *                <StackPanel Spacing="4" Orientation="Horizontal">
                                <FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xEB52;" />
                                <TextBlock Text="Save"/>
                            </StackPanel>
     */
    private void SaveButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ViewModel.SaveCommand.Execute(new ModifyLibraryCommand(Seq1(ViewModel.Id), !ViewModel.IsSaved));
    }

    public object SavedToContent(bool b)
    {
        var stckp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        if (b)
        {
            stckp.Children.Add(new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\uEB52"
            });
            stckp.Children.Add(new TextBlock
            {
                Text = "Remove"
            });
        }
        else
        {
            stckp.Children.Add(new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE006" });
            stckp.Children.Add(new TextBlock { Text = "Save" });
        }

        return stckp;
    }

    private void ALbumHeaderContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        Point point = new Point(0, 0);
        var properFlyout = ViewModel.Id.ConstructFlyout();
        if (args.TryGetPosition(sender, out point))
        {
            properFlyout.ShowAt(sender, point);
        }
        else
        {
            properFlyout.ShowAt((FrameworkElement)sender);
        }
    }

    private void MainScroller_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var progress = Math.Clamp(MainScroller.VerticalOffset / 100, 0, 1);
        OverlayRectangle.Opacity = progress >= 1 ? 1 : 0;
    }

    private async void PlayEntireAlbumTappped(object sender, TappedRoutedEventArgs e)
    {
        var context = new PlayContextStruct(
            ContextId: ViewModel.Id.ToString(),
             Index: 0,
            TrackId: Option<AudioId>.None,
            ContextUrl: $"context://{ViewModel.Id}",
            NextPages: Option<IEnumerable<ContextPage>>.None,
            PageIndex: Option<int>.None,
            Metadata: HashMap.empty<string, string>()
        );
        await ShellViewModel<WaveeUIRuntime>.Instance.Playback.PlayContextAsync(context);
    }
}
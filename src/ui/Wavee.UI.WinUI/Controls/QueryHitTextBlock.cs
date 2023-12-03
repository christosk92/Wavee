using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Wavee.Spotify.Application.Search.Queries;

namespace Wavee.UI.WinUI.Controls;

public sealed class QueryHitTextBlock : Control
{
    public static readonly DependencyProperty TermsProperty = DependencyProperty.Register(nameof(Terms), typeof(IReadOnlyCollection<SpotifyAutocompleteQuerySegment>), typeof(QueryHitTextBlock), new PropertyMetadata(default(IReadOnlyCollection<SpotifyAutocompleteQuerySegment>), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
       var textBlock = (QueryHitTextBlock)d;
       textBlock.OnApplyTemplate();
    }

    public QueryHitTextBlock()
    {
        this.DefaultStyleKey = typeof(QueryHitTextBlock);
    }

    public IReadOnlyCollection<SpotifyAutocompleteQuerySegment> Terms
    {
        get => (IReadOnlyCollection<SpotifyAutocompleteQuerySegment>)GetValue(TermsProperty);
        set => SetValue(TermsProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var textBlock = (TextBlock)GetTemplateChild("PART_BLOCK");
        if (textBlock is null)
        {
            return;
        }

        textBlock.Inlines.Clear();
        if (Terms is null)
        {
            return;
        }

        foreach (var term in Terms)
        {
            var run = new Run
            {
                Text = term.Value
            };
            if (term.Matched)
            {
                // do nothing
            }
            else
            {
                //slight gray
                run.Foreground = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ApplicationSecondaryForegroundThemeBrush"];
            }

            textBlock.Inlines.Add(run);
        }
    }
}
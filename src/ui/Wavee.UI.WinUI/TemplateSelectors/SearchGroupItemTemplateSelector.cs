using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.WinUI.TemplateSelectors;

internal class SearchGroupItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate TopResults { get; set; } = null!;
    public DataTemplate Horizontal { get; set; } = null!;
    public DataTemplate Tracks { get; set; } = null!;
    public DataTemplate Artist { get; set; } = null!;
    public DataTemplate Square { get; set; } = null!;
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            SearchGroupViewModel v => v.RenderingType switch
            {
                SearchGroupRenderingType.TopResults => TopResults,
                SearchGroupRenderingType.Horizontal => Horizontal,
                SearchGroupRenderingType.Tracks => Tracks,
                _ => throw new ArgumentOutOfRangeException()
            },
            SearchItemViewModel v => v.IsArtist switch
            {
                true => Artist,
                false => Square
            }
        };
    }
}
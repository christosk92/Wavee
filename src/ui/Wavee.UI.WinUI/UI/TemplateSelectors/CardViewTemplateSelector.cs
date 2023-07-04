using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Common;
using Wavee.UI.WinUI.Components;

namespace Wavee.UI.WinUI.UI.TemplateSelectors;

public sealed class CardViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate CardViewTemplate { get; set; } = null!;
    public DataTemplate PodcastViewTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            CardViewModel => CardViewTemplate,
            PodcastEpisodeCardViewModel => PodcastViewTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}
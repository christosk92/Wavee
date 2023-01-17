using Eum.UI.ViewModels.Search;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI;

public class GetSecondColumnTypeSelector : DataTemplateSelector
{
    public DataTemplate Songs { get; set; }
    public DataTemplate Recs { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        switch (item)
        {
            case SongsResultGroup:
                return Songs;
            case RecommendationsResultGroup:
                return Recs;
        }
        return base.SelectTemplateCore(item, container);
    }
}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModel.Search;

namespace Wavee.UI.WinUI;

public class SearchContainerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate TopHitTracksComposite { get; set; }
    public DataTemplate Regular { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ((SearchItemGroup)item).Title switch
        {
            "topHit" => TopHitTracksComposite,
            _ => Regular
        };
        return base.SelectTemplateCore(item, container);
    }
}
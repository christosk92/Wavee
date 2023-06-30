using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.WinUI.Models;

namespace Wavee.UI.WinUI;

public class AlbumPageTrackTemplateSelector : DataTemplateSelector
{
    public DataTemplate ShimmerTrackTemplate { get; set; }
    public DataTemplate TrackTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is null)
        {
            return null;
        }

        return item is ShimmerTrackModel ? ShimmerTrackTemplate : TrackTemplate;
    }
}
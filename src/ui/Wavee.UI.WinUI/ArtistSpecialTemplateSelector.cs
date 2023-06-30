using System;
using Google.Protobuf.WellKnownTypes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Wavee.Metadata.Artist;

namespace Wavee.UI.WinUI;

public class ArtistSpecialTemplateSelector : DataTemplateSelector
{
    public DataTemplate PinnedConcertTemplate { get; set; }
    public DataTemplate PinnedReleaseTemplate { get; set; }
    public DataTemplate PreReleaseTemplate { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ArtistOverviewPinnedConcert => PinnedConcertTemplate,
            ArtistOverviewPinnedItem => PinnedReleaseTemplate,
            IArtistPreReleaseItem => PreReleaseTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}
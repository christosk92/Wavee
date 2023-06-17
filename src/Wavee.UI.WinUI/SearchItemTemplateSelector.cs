using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Core.Contracts.Search;
using Wavee.UI.ViewModel.Search;

namespace Wavee.UI.WinUI;

public class SearchItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate HighlightedTemplate { get; set; }
    public DataTemplate RecommendedTemplate { get; set; }
    public DataTemplate TrackTemplate { get; set; }
    public DataTemplate AlbumTemplate { get; set; }
    public DataTemplate ArtistTemplate { get; set; }
    public DataTemplate PlaylistTemplate { get; set; }
    public DataTemplate PodcastShowTemplate { get; set; }
    public DataTemplate PodcastEpisodeTemplate { get; set; }
    public DataTemplate UnknownTemplate { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is GroupedSearchResult search)
        {
            switch (search.Group)
            {
                case SearchGroup.Highlighted:
                    return HighlightedTemplate;
                case SearchGroup.Recommended:
                    return RecommendedTemplate;
                case SearchGroup.Track:
                    return TrackTemplate;
                case SearchGroup.Album:
                    return AlbumTemplate;
                case SearchGroup.Artist:
                    return ArtistTemplate;
                case SearchGroup.Playlist:
                    return PlaylistTemplate;
                case SearchGroup.PodcastShow:
                    return PodcastShowTemplate;
                case SearchGroup.PodcastEpisode:
                    return PodcastEpisodeTemplate;
                case SearchGroup.Unknown:
                    return UnknownTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return base.SelectTemplateCore(item, container);
    }
}
using Wavee.Core.Ids;
using Wavee.UI.Core.Contracts.Common;

namespace Wavee.UI.Core.Contracts.Search;

public interface ISearchClient 
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken token);
}


public readonly record struct SearchResultKey(AudioId Id, SearchGroup Type);
public readonly record struct SearchResult(SearchResultKey Key, CardItem Item);
public enum SearchGroup
{
    Highlighted,
    Recommended,
    Track,
    Album,
    Artist,
    Playlist,
    PodcastShow,
    PodcastEpisode,
    Unknown,
}
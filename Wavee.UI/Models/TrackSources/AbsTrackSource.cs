using CommunityToolkit.Common.Collections;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.Models.TrackSources;
public abstract class AbsTrackSource<T> : IIncrementalSource<T>
{
    public SortOption SortBy
    {
        get;
        set;
    }
    public bool Ascending
    {
        get;
        set;
    }
    public bool HeartedFilter
    {
        get;
        set;
    }

    public abstract Task<IEnumerable<T>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken());
}

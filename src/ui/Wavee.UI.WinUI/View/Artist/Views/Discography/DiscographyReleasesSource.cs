using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Common.Collections;
using Wavee.Metadata.Artist;
using Wavee.UI.Client.Artist;

namespace Wavee.UI.WinUI.View.Artist.Views.Discography;

public sealed class DiscographyReleasesSource : IIncrementalSource<IArtistDiscographyRelease>
{
    private readonly GetReleases _getReleasesFunc;
    public DiscographyReleasesSource(GetReleases getReleasesFunc)
    {
        _getReleasesFunc = getReleasesFunc;
    }

    public Task<IEnumerable<IArtistDiscographyRelease>> GetPagedItemsAsync(int pageIndex, 
        int pageSize, CancellationToken cancellationToken = new CancellationToken())
    {
        var offset = pageIndex * pageSize;
        var limit = pageSize;
        return _getReleasesFunc(offset, limit, cancellationToken);
    }
}
using Wavee.Core.Ids;

namespace Wavee.UI.Core.Contracts.Album;

public interface IAlbumView
{
    Task<AlbumView> GetAlbumViewAsync(AudioId id, CancellationToken ct = default);
}
using Wavee.Core.Ids;

namespace Wavee.UI.Core.Contracts.Artist;

public interface IArtistView
{
    Task<SpotifyArtistView> GetArtistViewAsync(AudioId id, CancellationToken ct = default);
}
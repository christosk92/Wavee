using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.Core.Contracts.Artist;

public interface IArtistView
{
    Aff<SpotifyArtistView> GetArtistViewAsync(AudioId id, CancellationToken ct = default);
}
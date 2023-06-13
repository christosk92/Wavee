using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.UI.Core.Contracts.Metadata;

public interface IMetadataClient
{
    Task<TrackOrEpisode> GetItem(AudioId id, CancellationToken ct = default);
    Aff<SpotifyColors> GetColorForImage(string imageUrl, CancellationToken ct = default);
}
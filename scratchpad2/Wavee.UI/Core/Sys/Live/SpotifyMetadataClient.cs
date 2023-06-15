using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.UI.Core.Contracts.Metadata;
using static LanguageExt.Prelude;
namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyMetadataClient : IMetadataClient
{
    private readonly SpotifyClient _client;
    public SpotifyMetadataClient(SpotifyClient client)
    {
        _client = client;
    }

    public Aff<TrackOrEpisode> GetItem(AudioId id, CancellationToken ct = default)
    {
        var country = _client.CountryCode.IfNone("US");

        var res =
            from potentialCache in _client.Cache.Get(id)
                .Match(
                    Some: x => SuccessAff(x),
                    None: () =>
                        from fetched in _client.Mercury.GetMetadata(id, country, ct).ToAff()
                        from _ in Eff(() => _client.Cache.Save(fetched))
                        select fetched
                )
            select potentialCache;

        return res;
    }

    public Aff<SpotifyColors> GetColorForImage(string imageUrl, CancellationToken ct = default)
    {
        var res =
            from potentialCache in _client.Cache.GetColorFor(imageUrl)
                .Match(
                    Some: x => SuccessAff(x),
                    None: () =>
                        from fetched in _client.PrivateApi.FetchColorFor(Seq1(imageUrl), ct).ToAff()
                        from _ in Eff(() => _client.Cache.SaveColorFor(imageUrl, fetched))
                        select fetched
                )
            select potentialCache;

        return res;
    }
}
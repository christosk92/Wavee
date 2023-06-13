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

    public Task<TrackOrEpisode> GetItem(AudioId id, CancellationToken ct = default)
    {
        var country = _client.CountryCode.IfNone("US");
        return _client.Mercury.GetMetadata(id, country, ct);
    }

    public Aff<SpotifyColors> GetColorForImage(string imageUrl, CancellationToken ct = default)
    {
        return _client.PrivateApi.FetchColorFor(Seq1(imageUrl), ct)
            .ToAff();
    }
}
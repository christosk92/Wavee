using System.Text.Json;
using LanguageExt;
using LanguageExt.Pipes;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.UI.Core.Contracts.Artist;
using static LanguageExt.Prelude;
namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyArtistClient : IArtistView
{
    private readonly SpotifyClient _client;

    public SpotifyArtistClient(SpotifyClient client)
    {
        _client = client;
    }

    public Aff<SpotifyArtistViewV2> GetArtistViewAsync(AudioId id, CancellationToken ct = default)
    {
        var aff =
            from adapted in _client.Cache.GetRawEntity(id.ToString())
                .Match(
                    Some: x => SuccessAff(SpotifyArtistViewV2.FromCache(x)),
                    None: () =>
                        from fetched in FetchArtistAsTask(id, ct).ToAff()
                        from adapted in Eff(() => SpotifyArtistViewV2.ParseFrom(fetched))
                        from _ in Eff(() => _client.Cache.SaveRawEntity(id.ToString(), id.ToBase16(), JsonSerializer.SerializeToUtf8Bytes(adapted),
                            DateTimeOffset.UtcNow.AddDays(1)))
                        select adapted
                )
            select adapted;
        return aff;
    }


    //New: GraphQL (Shit!)
    private Task<Stream> FetchArtistAsTask(AudioId id, CancellationToken ct)
    {
        return _client.PrivateApi.GetArtistOverviewAsync(id, ct);
    }

    //OLD (mercury)
    // private async Task<ReadOnlyMemory<byte>> FetchArtistAsTask(AudioId id, CancellationToken ct)
    // {
    //     var idStr = id.ToBase62();
    //     const string fetch_uri = "hm://artistview/v1/artist/{0}";
    //     var uri = string.Format(fetch_uri, idStr);
    //
    //     var response = await _client.Mercury.Get(uri, ct);
    //     if (response.Header.StatusCode != 200)
    //     {
    //         throw new MercuryException(response);
    //     }
    //     return response.Payload;
    // }
}

public sealed class MercuryException : Exception
{
    public MercuryException(MercuryPacket response) : base($"Received non-200 ({response.Header.StatusCode}) response from Mercury for {response.Header.Uri}")
    {

    }
}
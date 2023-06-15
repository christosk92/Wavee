using System.Text.Json;
using LanguageExt;
using LanguageExt.Pipes;
using Wavee.Core.Ids;
using Wavee.Spotify;
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

    public Aff<SpotifyArtistView> GetArtistViewAsync(AudioId id, CancellationToken ct = default)
    {
        var aff =
            from potentialCache in _client.Cache.GetRawEntity(id.ToString())
                .Match(
                    Some: x => SuccessAff(x),
                    None: () =>
                        from fetched in FetchArtistAsTask(id, ct).ToAff()
                        from _ in Eff(() => _client.Cache.SaveRawEntity(id.ToString(), id.ToBase16(), fetched.ToArray(),
                            DateTimeOffset.UtcNow.AddDays(1)))
                        select fetched
                )
            from adapted in Eff(() =>
            {
                using var jsonDoc = JsonDocument.Parse(potentialCache);
                return SpotifyArtistView.From(jsonDoc, id);
            })
            select adapted;

        return aff;
    }

    private async Task<ReadOnlyMemory<byte>> FetchArtistAsTask(AudioId id, CancellationToken ct)
    {
        var idStr = id.ToBase62();
        const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
        var uri = string.Format(fetch_uri, idStr, "en");

        var response = await _client.Mercury.Get(uri, ct);
        return response.Payload;
    }
}
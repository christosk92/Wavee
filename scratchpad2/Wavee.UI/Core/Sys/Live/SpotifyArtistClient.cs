using System.Text.Json;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.Core.Contracts.Artist;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyArtistClient : IArtistView
{
    private readonly SpotifyClient _client;

    public SpotifyArtistClient(SpotifyClient client)
    {
        _client = client;
    }

    public async Task<SpotifyArtistView> GetArtistViewAsync(AudioId id, CancellationToken ct = default)
    {
        var idStr = id.ToBase62();
        const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
        var uri = string.Format(fetch_uri, idStr, "en");

        var response = await _client.Mercury.Get(uri, ct);
        using var jsonDoc = JsonDocument.Parse(response.Payload);
        return SpotifyArtistView.From(jsonDoc, id);
    }
}
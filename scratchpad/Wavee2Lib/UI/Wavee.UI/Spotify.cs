using System.Net.Http.Headers;
using System.Text.Json;
using LanguageExt;
using Wavee.Infrastructure.IO;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Mercury;
using static LanguageExt.Prelude;
namespace Wavee.UI;

internal static class Spotify
{
    public static Aff<JsonDocument> GetHomeAsync(
        this SpotifyClient client,
        string types, int limit, int offset,
        int contentLimit, int contentOffset,
        CancellationToken ct)
    {
        var apiurl =
            $"https://api.spotify.com/v1/views/desktop-home?types={types}&offset={offset}&limit={limit}&content_limit={contentLimit}&content_offset={contentOffset}";

        return
            from bearer in client.Mercury.GetAccessToken(ct).ToAff()
                .Map(x => new AuthenticationHeaderValue("Bearer", x))
            from result in HttpIO.Get(apiurl, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
                .ToAff().MapAsync(async r =>
                {
                    await using var stream = await r.Content.ReadAsStreamAsync(ct);
                    return await JsonDocument.ParseAsync(stream, default, ct);
                })
            select result;
    }

    public static Aff<MercuryPacket> GetLibarryComponent(this SpotifyClient client, string key, string userId,
        CancellationToken ct) =>
        from mercury in SuccessEff(client.Mercury)
        from tracksAndAlbums in mercury.Get(
            $"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&", ct).ToAff()
        select tracksAndAlbums;
}
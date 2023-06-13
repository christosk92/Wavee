using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageExt;
using Wavee.Infrastructure.IO;
using Wavee.Spotify.Infrastructure.PublicApi.Me;

namespace Wavee.Spotify.Infrastructure.PublicApi;

internal readonly struct SpotifyPublicApi : ISpotifyPublicApi
{
    private readonly Func<CancellationToken, Task<string>> _tokenFactory;

    public SpotifyPublicApi(Func<CancellationToken, Task<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }

    public async Task<PrivateSpotifyUser> GetMe(CancellationToken ct = default)
    {
        using var response = await Get(Endpoints.Me, ct);
        return await response.Content.ReadFromJsonAsync<PrivateSpotifyUser>(ct);
    }

    public async Task<HttpResponseMessage> GetDesktopHome(string types, int offset, int limit, int contentLimit, int contentOffset,
        CancellationToken ct = default)
    {
        var beare = await _tokenFactory(ct);
        
        var endpoint = string.Format(Endpoints.DesktopHome, types, offset, limit, contentLimit, contentOffset);

        var response = await HttpIO.Get(endpoint, new AuthenticationHeaderValue("Bearer", beare), HashMap<string, string>.Empty, ct);
        return response;
    }

    private async Task<HttpResponseMessage> Get(string endpoint, CancellationToken ct)
    {
        var bearer = await _tokenFactory(ct);

        var response = await HttpIO.Get(endpoint, new AuthenticationHeaderValue("Bearer", bearer), HashMap<string, string>.Empty, ct);
        return response;
    }


    private static class Endpoints
    {
        private const string Base = "https://api.spotify.com/v1";

        public static string Me => $"{Base}/me";
        public static string DesktopHome => $"{Base}/views/desktop-home?types={{0}}&offset={{1}}&limit={{2}}&content_limit={{3}}&content_offset={{4}}";
    }
}
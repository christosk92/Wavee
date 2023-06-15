using System.Text.Json;
using System.Web;
using Wavee.Spotify;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Core.Contracts.Home;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyHomeClient : IHomeView
{
    private readonly SpotifyClient _client;

    public SpotifyHomeClient(SpotifyClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<HomeGroup>> GetHomeViewAsync(string type, int limit,
        int contentLimit, CancellationToken none)
    {
        type = HttpUtility.UrlEncode(type);
        using var response = await _client.PublicApi.GetDesktopHome(type, 0, limit, contentLimit, 0, none);
        await using var stream = await response.Content.ReadAsStreamAsync(none);
        using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: none);
        return HomeGroup.ParseFrom(jsonDoc);
    }
}
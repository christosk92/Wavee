using Eum.Spotify.playlist4;
using Eum.Spotify.playlists;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Playlist;

public sealed class SpotifyPlaylistClient : ISpotifyPlaylistClient
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyTcpHolder _tcpHolder;

    public SpotifyPlaylistClient(IHttpClientFactory httpClientFactory, SpotifyTcpHolder tcpHolder)
    {
        _tcpHolder = tcpHolder;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async Task<SelectedListContent> GetRootList(CancellationToken cancellationToken)
    {
        const string endpoint =
            "https://spclient.com/playlist/v2/user/{0}/rootlist?decorate=revision,length,attributes,timestamp,owner";
        var user = _tcpHolder.WelcomeMessage.Result.CanonicalUsername;
        var url = string.Format(endpoint, user);
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rootlist = SelectedListContent.Parser.ParseFrom(stream);
        return rootlist;
    }

    public async Task<ulong> GetPopCount(SpotifyId fromUri, CancellationToken cancellationToken)
    {
        const string endpoint = "https://spclient.com/popcount/v2/playlist/{0}/count";
        var url = string.Format(endpoint, fromUri.ToBase62());
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var popCount = PopcountResult.Parser.ParseFrom(stream);

        return (ulong)(popCount.HasCount ? popCount.Count : 0);
    }

    public async Task<SelectedListContent> GetPlaylist(SpotifyId fromUri, CancellationToken cancellationToken)
    {
        const string endpoint = "https://spclient.com/playlist/v2/playlist/{0}";
        var url = string.Format(endpoint, fromUri.ToBase62());
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var playlist = SelectedListContent.Parser.ParseFrom(stream);
        return playlist;
    }
}

public interface ISpotifyPlaylistClient
{
    Task<SelectedListContent> GetRootList(CancellationToken cancellationToken);
    Task<ulong> GetPopCount(SpotifyId fromUri, CancellationToken cancellationToken);
    Task<SelectedListContent> GetPlaylist(SpotifyId fromUri, CancellationToken cancellationToken);
}
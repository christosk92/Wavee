using System.Text;
using System.Text.Json;
using Eum.Spotify.playlist4;
using Eum.Spotify.playlists;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Playlist;

internal sealed class SpotifyPlaylistClient : ISpotifyPlaylistClient
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyTcpHolder _tcpHolder;
    private readonly List<SpotifyPlaylistChangeListener> _changeListeners = new();

    public SpotifyPlaylistClient(IHttpClientFactory httpClientFactory, 
        SpotifyTcpHolder tcpHolder,
        SpotifyRemoteHolder remoteHolder)
    {
        _tcpHolder = tcpHolder;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);

        remoteHolder.PlaylistChanged += RemoteHolderOnPlaylistChanged;
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

    public async Task<ulong?> GetPopCount(SpotifyId fromUri, CancellationToken cancellationToken)
    {
        const string endpoint = "https://spclient.com/popcount/v2/playlist/{0}/count";
        var url = string.Format(endpoint, fromUri.ToBase62());
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var popCount = PopcountResult.Parser.ParseFrom(stream);

        return ((popCount.HasCount && !popCount.CountHiddenFromUsers) ? (ulong)popCount.Count : null);
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

    public SpotifyPlaylistChangeListener ChangeListener(SpotifyId id)
    {
        var listener = new SpotifyPlaylistChangeListener(id);
        _changeListeners.Add(listener);
        return listener;
    }

    private void RemoteHolderOnPlaylistChanged(object? sender, PlaylistModificationInfo playlistModificationInfo)
    {
        foreach (var listener in _changeListeners)
        {
            if (listener.Id.ToString() == Encoding.UTF8.GetString(playlistModificationInfo.Uri.Span))
            {
                listener.Incoming(playlistModificationInfo);
            }
        }
    }
}

public interface ISpotifyPlaylistClient
{
    Task<SelectedListContent> GetRootList(CancellationToken cancellationToken);
    Task<ulong?> GetPopCount(SpotifyId fromUri, CancellationToken cancellationToken);
    Task<SelectedListContent> GetPlaylist(SpotifyId fromUri, CancellationToken cancellationToken);
    SpotifyPlaylistChangeListener ChangeListener(SpotifyId id);
}

public sealed class SpotifyPlaylistChangeListener
{
    internal SpotifyPlaylistChangeListener(SpotifyId id)
    {
        Id = id;
    }
    public SpotifyId Id { get; }

    public event EventHandler<PlaylistModificationInfo>? ItemsChanged;

    public void Incoming(PlaylistModificationInfo playlistModificationInfo)
    {
        ItemsChanged?.Invoke(this, playlistModificationInfo);
    }
}
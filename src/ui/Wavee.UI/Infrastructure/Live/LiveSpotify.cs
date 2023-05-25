using Eum.Spotify;
using LanguageExt;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Eum.Spotify.playlist4;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Remote;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using static LanguageExt.Prelude;
using System.Threading;
using Google.Protobuf;
using Wavee.Spotify.Infrastructure.ApResolver;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Spotify.Collection.Proto.V2;
using Wavee.Core.Ids;
using Wavee.Core.Infrastructure.IO;

namespace Wavee.UI.Infrastructure.Live;

internal sealed class LiveSpotify : Traits.SpotifyIO
{
    private Option<SpotifyClient> _connection = Option<SpotifyClient>.None;
    private readonly SpotifyConfig _config;

    public LiveSpotify(SpotifyConfig config)
    {
        _config = config;
    }

    public Aff<SelectedListContent> GetRootList(CancellationToken ct) =>
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in ApResolve.GetSpClient(ct).ToAff()
            .Map(x => $"https://{x.host}:{x.port}")
            .Map(x =>
                $"{x}/playlist/v2/user/{client.WelcomeMessage.CanonicalUsername}/rootlist?decorate=revision,length,attributes,timestamp,owner")
        from bearer in client.TokenClient.GetToken(ct).ToAff().Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.GetAsync(spclient, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
            .ToAff().MapAsync(async r =>
            {
                await using var stream = await r.Content.ReadAsStreamAsync(ct);
                return SelectedListContent.Parser.ParseFrom(stream);
            })
        select result;

    public Aff<JsonDocument> FetchDesktopHome(string types, int limit, int offset,
        int contentLimit, int contentOffset,
        CancellationToken ct) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let apiurl = $"https://api.spotify.com/v1/views/desktop-home?types={types}&offset={offset}&limit={limit}&content_limit={contentLimit}&content_offset={contentOffset}"
        from bearer in client.TokenClient.GetToken(ct).ToAff().Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.GetAsync(apiurl, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
            .ToAff().MapAsync(async r =>
            {
                await using var stream = await r.Content.ReadAsStreamAsync(ct);
                return await JsonDocument.ParseAsync(stream, default, ct);
            })
        select result;

    public Aff<T> GetFromPublicApi<T>(string endpoint, CancellationToken cancellation) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let apiUrl = $"https://api.spotify.com/v1{endpoint}"
        from bearer in client.TokenClient.GetToken(cancellation).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.GetAsync(apiUrl, bearer, LanguageExt.HashMap<string, string>.Empty, cancellation)
            .ToAff().MapAsync(async r =>
            {
                var result = await r.Content.ReadFromJsonAsync<T>(cancellationToken: cancellation);
                r.Dispose();
                return result;
            })
        select result;

    public Aff<Unit> AddToPlaylist(AudioId playlistId, AudioId[] audioIds, Option<int> position) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let apiurl = $"https://api.spotify.com/v1/playlists/{playlistId.ToBase62()}/tracks"
        from bearer in client.TokenClient.GetToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from content in Eff(() =>
        {
            if (position.IsSome)
            {
                var r = new
                {
                    uris = audioIds.Select(c => c.ToString()),
                    position = position.ValueUnsafe()
                };
                var str = new StringContent(JsonSerializer.Serialize(r));
                str.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return str;
            }
            else
            {
                var r = new
                {
                    uris = audioIds.Select(c => c.ToString()),
                };
                var str = new StringContent(JsonSerializer.Serialize(r));
                str.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return str;
            }
        })
        from posted in HttpIO.Post(apiurl, bearer, content, CancellationToken.None)
            .ToAff()
            .Map(x => x.EnsureSuccessStatusCode())
        select unit;

    public Aff<Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct) =>
        //https://spclient.wg.spotify.com/collection/v2/write
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in ApResolve.GetSpClient(ct).ToAff()
            .Map(x => $"https://{x.host}:{x.port}")
            .Map(x =>
                $"{x}/collection/v2/write")
        from bearer in client.TokenClient.GetToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from content in Eff(() =>
        {
            var byteArrCnt = new ByteArrayContent(writeRequest.ToByteArray());
            byteArrCnt.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.collection-v2.spotify.proto");
            return byteArrCnt;
        })
        from posted in HttpIO.Post(spclient, bearer, content, CancellationToken.None)
            .ToAff()
            .Map(x => x.EnsureSuccessStatusCode())
        select unit;

    public async ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default)
    {
        var core = await SpotifyClient.CreateAsync(credentials, _config, ct);
        _connection = Some(core);
        return Unit.Default;
    }

    public Option<APWelcome> WelcomeMessage()
    {
        var maybe = _connection.Map(x => x.WelcomeMessage);
        return maybe;
    }

    public Option<IObservable<SpotifyRootlistUpdateNotification>> ObserveRootlist()
    {
        return _connection
            .Map(x => x.RemoteClient.RootlistChanged);
    }

    public Option<IObservable<SpotifyLibraryUpdateNotification>> ObserveLibrary()
    {
        return _connection
            .Map(x => x.RemoteClient.LibraryChanged);
    }


    public Option<IObservable<SpotifyRemoteState>> ObserveRemoteState()
    {
        return _connection
            .Map(x => x.RemoteClient.StateChanged);
    }

    public Option<SpotifyCache> Cache()
    {
        return _connection
            .Map(x => x.Cache);
    }

    public Option<string> CountryCode()
    {
        return _connection
            .Bind(x => x.CountryCode);
    }

    public Option<string> CdnUrl()
    {
        return _connection
            .Bind(x => x.ProductInfo.Find("image_url"));
    }

    public MercuryClient Mercury()
    {
        return _connection
            .Map(x => x.MercuryClient)
            .IfNone(() => throw new InvalidOperationException("Mercury client not available"));
    }

    public Option<string> GetOwnDeviceId()
    {
        return _connection
            .Map(x => x.DeviceId);
    }

    public Option<SpotifyRemoteClient> GetRemoteClient()
    {
        return _connection
            .Map(x => x.RemoteClient);
    }
}
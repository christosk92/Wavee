using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.ApResolver;

namespace Wavee.Spotify.Clients.SpApi;

public interface ISpApi
{
    ValueTask<StorageResolveResponse> GetAudioStorage(ByteString fileId, CancellationToken ct = default);
}

internal readonly struct SpApiClientImpl<RT> : ISpApi where RT : struct, HasHttp<RT>
{
    private static Option<string> _apUrl = Option<string>.None;
    private static Ref<HashMap<string, BearerToken>> _bearerCache = Ref(LanguageExt.HashMap<string, BearerToken>.Empty);

    private readonly IMercuryClient _mercury;
    private readonly RT _runtime;
    private readonly string _userId;

    public SpApiClientImpl(IMercuryClient mercury, RT runtime, string userId)
    {
        _mercury = mercury;
        _runtime = runtime;
        _userId = userId;
    }

    public async ValueTask<StorageResolveResponse> GetAudioStorage(ByteString fileId, CancellationToken ct = default)
    {
        var affResult = await GetAudioStorageInternal(fileId, _mercury, _userId, _bearerCache, ct).Run(_runtime);
        return affResult.Match(
            Succ: x => x,
            Fail: ex => throw ex
        );
    }

    // public async ValueTask<HttpResponseMessage> GetChunk(string cdnUrlUrl, int chunkNumber, int chunkSize,
    //     CancellationToken ct = default)
    // {
    //     var affResult = await GetChunkInternal(cdnUrlUrl, chunkNumber, chunkSize, ct).Run(_runtime);
    //     return affResult.Match(
    //         Succ: x => x,
    //         Fail: ex => throw ex
    //     );
    // }
    //
    // private Aff<RT, HttpResponseMessage> GetChunkInternal(string cdnUrlUrl, int chunkNumber, int chunkSize,
    //     CancellationToken ct) =>
    //     from bearer in FetchBearer(mercuryClient, userId, bearerCache)
    //         .Map(x => new AuthenticationHeaderValue("Bearer", x))
    //

    private static Aff<RT, StorageResolveResponse> GetAudioStorageInternal(ByteString fileId,
        IMercuryClient mercuryClient,
        string userId,
        Ref<HashMap<string, BearerToken>> bearerCache,
        CancellationToken ct = default) =>
        from baseUrl in _apUrl.Match(
            Some: x => SuccessAff(x),
            None: () =>
            {
                var map1 = GetSpClientUrl(ApResolve)
                    .Map(x => $"https://{x}")
                    .Map(x =>
                    {
                        _apUrl = x;
                        return x;
                    });
                return map1;
            })
        from bearer in FetchBearer(mercuryClient, userId, bearerCache)
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        let url = $"{baseUrl}/storage-resolve/files/audio/interactive/{ToBase16(fileId.Span)}"
        from response in Http<RT>.Get(url, bearer, Option<HashMap<string, string>>.None, ct)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                await using var data = await x.Content.ReadAsStreamAsync(ct);
                return StorageResolveResponse.Parser.ParseFrom(data);
            })
        select response;

    public static string ToBase16(ReadOnlySpan<byte> fileIdSpan)
    {
        Span<byte> buffer = new byte[40];
        var i = 0;
        foreach (var v in fileIdSpan)
        {
            buffer[i] = BASE16_DIGITS[v >> 4];
            buffer[i + 1] = BASE16_DIGITS[v & 0x0f];
            i += 2;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private static readonly byte[] BASE16_DIGITS = "0123456789abcdef".Select(c => (byte)c).ToArray();

    private static Aff<RT, string> FetchBearer(IMercuryClient mercuryClient, string userId,
        Ref<HashMap<string, BearerToken>> cache)
    {
        var cacheMaybe = cache.Value.Find(userId).Bind(x => !x.Expired ? Some(x) : None);
        if (cacheMaybe.IsSome)
            return SuccessAff(cacheMaybe.ValueUnsafe().AccessToken);

        return
            from token in FetchBearerToken(mercuryClient)
            from newCache in Eff<HashMap<string, BearerToken>>(() => atomic(() => cache.Swap(f =>
            {
                var k = f.AddOrUpdate(userId, token);
                return k;
            })))
            select newCache.Find(userId).ValueUnsafe().AccessToken;
    }

    private static Aff<RT, BearerToken> FetchBearerToken(IMercuryClient mercuryClient)
    {
        const string keymasterurl = "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
        const string scopes =
            "app-remote-control,playlist-modify,playlist-modify-private,playlist-modify-public,playlist-read,playlist-read-collaborative,playlist-read-private,streaming,ugc-image-upload,user-follow-modify,user-follow-read,user-library-modify,user-library-read,user-modify,user-modify-playback-state,user-modify-private,user-personalized,user-read-birthdate,user-read-currently-playing,user-read-email,user-read-play-history,user-read-playback-position,user-read-playback-state,user-read-private,user-read-recently-played,user-top-read";
        const string clientId = SpotifyConstants.KEYMASTER_CLIENT_ID;
        var url = string.Format(keymasterurl, scopes, clientId);

        return
            from response in mercuryClient.Send(MercuryMethod.Get, url, None).ToAff()
            from bearerToken in Eff(() => BearerToken.ParseFrom(response.Body))
            select bearerToken;
    }

    const string ApResolve = "https://apresolve.spotify.com/?type=spclient";

    private static Aff<RT, string> GetSpClientUrl(string url) =>
        from response in Http<RT>.Get(ApResolve, None, Option<HashMap<string, string>>.None)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                var data = await x.Content.ReadFromJsonAsync<ApResolveData>();
                return data;
            })
        select response.SpClient.First();
}

internal readonly record struct BearerToken(
    string AccessToken,
    TimeSpan ExpiresIn,
    string TokenType,
    string[] Scopes,
    DateTimeOffset Timestamp
)
{
    private const int EXPIRY_THRESHOLD_S = 20;

    public bool Expired => Timestamp + (ExpiresIn - TimeSpan.FromSeconds(EXPIRY_THRESHOLD_S))
                           < DateTimeOffset.Now;

    public const string MERCURY_TOKEN_TYPE = "mercury";

    public static BearerToken ParseFrom(ReadOnlyMemory<byte> bodySpan)
    {
        using var jsonDocument = JsonDocument.Parse(bodySpan);
        var root = jsonDocument.RootElement;

        var accessToken = root.GetProperty("accessToken").GetString();
        var expiresIn = root.GetProperty("expiresIn").GetInt32();
        var tokenType = root.GetProperty("tokenType").GetString();
        var scopes = root.GetProperty("scope").EnumerateArray().Select(x => x.GetString()).ToArray();
        var timestamp = DateTimeOffset.Now;

        return new BearerToken(accessToken, TimeSpan.FromSeconds(expiresIn), tokenType, scopes, timestamp);
    }
}
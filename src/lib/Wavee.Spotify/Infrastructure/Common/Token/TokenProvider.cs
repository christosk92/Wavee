using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Constants;
using Wavee.Spotify.Helpers.Extensions;
using Wavee.Spotify.Infrastructure.Common.Mercury;
using Wavee.Spotify.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.Common.Token;

internal readonly struct TokenProvider<RT> : ITokenProvider where RT : struct, HasTCP<RT>
{
    private readonly RT _runtime;
    private readonly MercuryClient<RT> _mercury;

    private readonly Func<Option<string>> _hasTokenInCache;
    private readonly Func<MercuryTokenData, Unit> _cache;

    public TokenProvider(MercuryClient<RT> mercury, RT runtime, Func<Option<string>> hasTokenInCache,
        Func<MercuryTokenData, Unit> cache)
    {
        _mercury = mercury;
        _runtime = runtime;
        _hasTokenInCache = hasTokenInCache;
        _cache = cache;
    }

    public ValueTask<string> GetToken()
    {
        var cachedToken = _hasTokenInCache();
        if (cachedToken.IsSome)
        {
            return new ValueTask<string>(cachedToken.ValueUnsafe());
        }

        var cacheFunc = _cache;
        var uri = string.Format(KEYMASTER_URI, string.Join(",", DefaultScopes), KEYMASTER_CLIENT_ID);
        var aff =
            from token in _mercury.Get(uri)
                .Map(c => c.DeserializeFromJson<MercuryTokenData>()
                    .Match(Some: t => t, None: () => throw new Exception("Failed to deserialize token")))
            let _ = cacheFunc(token)
            select token.AccessToken;

        return new ValueTask<string>(aff);
    }

    private static readonly string[] DefaultScopes = { "playlist-read" };

    private const string KEYMASTER_URI =
        "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
}
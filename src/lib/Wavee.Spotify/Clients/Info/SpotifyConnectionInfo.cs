using LanguageExt.Effects.Traits;

namespace Wavee.Spotify.Clients.Info;

internal readonly struct SpotifyConnectionInfo<RT> : ISpotifyConnectionInfo where RT : struct, HasCancel<RT>
{
    private readonly RT _runtime;
    private readonly Aff<RT, Option<string>> _countryCode;
    private readonly Aff<RT, Option<HashMap<string, string>>> _productInfo;

    public SpotifyConnectionInfo(RT runtime,
        Aff<RT, Option<string>> countryCode,
        Aff<RT, Option<HashMap<string, string>>> productInfo)
    {
        _runtime = runtime;
        _countryCode = countryCode;
        _productInfo = productInfo;
    }

    public ValueTask<Option<string>> CountryCode
    {
        get
        {
            var aff = _countryCode.Run(_runtime);
            var result = aff
                .Map(x => x.Match(
                    Succ: maybe => maybe,
                    Fail: _ => Option<string>.None
                ));
            return result;
        }
    }

    public ValueTask<Option<HashMap<string, string>>> ProductInfo
    {
        get
        {
            var aff = _productInfo.Run(_runtime);
            var result = aff
                .Map(x => x.Match(
                    Succ: maybe => maybe,
                    Fail: _ => Option<HashMap<string, string>>.None
                ));
            return result;
        }
    }
}
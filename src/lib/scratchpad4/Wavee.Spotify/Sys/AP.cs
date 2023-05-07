using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;

namespace Wavee.Spotify.Sys;

internal static class AP<RT> where RT : struct, HasHttp<RT>
{
    const string AP_URL = "https://apresolve.spotify.com/?type=accesspoint";
    const string SP_CLIENT_URL = "https://apresolve.spotify.com/?type=spclient";
    const string DEALER_CLIENT_UR = "https://apresolve.spotify.com/?type=dealer";

    private static Ref<Option<(string Host, ushort Port)>>
        FETCHED_AP_URL = Ref(Option<(string Host, ushort Port)>.None);

    private static Ref<Option<(string Host, ushort Port)>> FETCHED_SP_CLIENT_URL =
        Ref(Option<(string Host, ushort Port)>.None);

    private static Ref<Option<(string Host, ushort Port)>> FETCHED_DEALER_CLIENT_URL =
        Ref(Option<(string Host, ushort Port)>.None);


    public static Aff<RT, (string Host, ushort Port)> FetchAP()
    {
        if (!FETCHED_AP_URL.Value.IsNone)
        {
            return SuccessEff<RT, (string Host, ushort Port)>(FETCHED_AP_URL.Value.ValueUnsafe());
        }

        return from httpResponse in Http<RT>.Get(AP_URL, Option<AuthenticationHeaderValue>.None,
                Option<HashMap<string, string>>.None)
            from _ in Eff((() =>
            {
                httpResponse.EnsureSuccessStatusCode();
                return unit;
            }))
            from jsonContent in httpResponse.Content.ReadFromJsonAsync<ApResolveData>().ToAff()
                .Map(x => x.AccessPoint.First())
            from splitted in Eff(() =>
            {
                var split = jsonContent.Split(":", 2);
                return (split[0], ushort.Parse(split[1]));
            })
            from __ in atomic(Eff(() => FETCHED_AP_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(splitted))))
            select splitted;
    }

    public static Aff<RT, (string Host, ushort Port)> FetchSpClient()
    {
        if (!FETCHED_SP_CLIENT_URL.Value.IsNone)
        {
            return SuccessEff<RT, (string Host, ushort Port)>(FETCHED_SP_CLIENT_URL.Value.ValueUnsafe());
        }


        return from httpResponse in Http<RT>.Get(SP_CLIENT_URL, Option<AuthenticationHeaderValue>.None,
                Option<HashMap<string, string>>.None)
            from _ in Eff((() =>
            {
                httpResponse.EnsureSuccessStatusCode();
                return unit;
            }))
            from jsonContent in httpResponse.Content.ReadFromJsonAsync<ApResolveData>().ToAff()
                .Map(x => x.SpClient.First())
            from splitted in Eff(() =>
            {
                var split = jsonContent.Split(":", 2);
                return (split[0], ushort.Parse(split[1]));
            })
            from __ in atomic(Eff(() =>
                FETCHED_SP_CLIENT_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(splitted))))
            select splitted;
    }

    public static Aff<RT, (string Host, ushort Port)> FetchDealer()
    {
        if (!FETCHED_SP_CLIENT_URL.Value.IsNone)
        {
            return SuccessEff<RT, (string Host, ushort Port)>(FETCHED_DEALER_CLIENT_URL.Value.ValueUnsafe());
        }


        return from httpResponse in Http<RT>.Get(DEALER_CLIENT_UR, Option<AuthenticationHeaderValue>.None,
                Option<HashMap<string, string>>.None)
            from _ in Eff((() =>
            {
                httpResponse.EnsureSuccessStatusCode();
                return unit;
            }))
            from jsonContent in httpResponse.Content.ReadFromJsonAsync<ApResolveData>().ToAff()
                .Map(x => x.Dealer.First())
            from splitted in Eff(() =>
            {
                var split = jsonContent.Split(":", 2);
                return (split[0], ushort.Parse(split[1]));
            })
            from __ in atomic(Eff(() =>
                FETCHED_DEALER_CLIENT_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(splitted))))
            select splitted;
    }

    private readonly record struct ApResolveData(
        [property: JsonPropertyName("accesspoint")]
        string[] AccessPoint,
        [property: JsonPropertyName("dealer")] string[] Dealer,
        [property: JsonPropertyName("spclient")]
        string[] SpClient);
}
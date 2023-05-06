using System.Net.Http.Headers;
using System.Net.Http.Json;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.ApResolver;

internal static class AP<RT> where RT : struct, HasHttp<RT>
{
    const string DEALER_URL = "https://apresolve.spotify.com/?type=dealer";
    const string AP_URL = "https://apresolve.spotify.com/?type=accesspoint";
    const string SP_CLIENT_URL = "https://apresolve.spotify.com/?type=spclient";
    public static Aff<RT, (string Host, ushort Port)> FetchHostAndPort() =>
        from httpResponse in Http<RT>.Get(AP_URL, Option<AuthenticationHeaderValue>.None,
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
        select splitted;

    public static Aff<RT, (string Host, ushort Port)> FetchDealer() =>
        from httpResponse in Http<RT>.Get(DEALER_URL, Option<AuthenticationHeaderValue>.None,
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
        select splitted;

    public static Aff<RT, (string Host, ushort Port)> FetchSpClient() =>
        from httpResponse in Http<RT>.Get(SP_CLIENT_URL, Option<AuthenticationHeaderValue>.None,
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
        select splitted;
}
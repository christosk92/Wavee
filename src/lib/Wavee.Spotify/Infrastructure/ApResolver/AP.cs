using System.Net.Http.Headers;
using System.Net.Http.Json;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.ApResolver;

internal static class AP<RT> where RT : struct, HasHttp<RT>
{
    public static Aff<RT, (string Host, ushort Port)> FetchHostAndPort(string url) =>
        from httpResponse in Http<RT>.Get(url, Option<AuthenticationHeaderValue>.None,
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
}
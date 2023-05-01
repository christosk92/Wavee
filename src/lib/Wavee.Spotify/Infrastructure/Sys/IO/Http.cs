using System.Diagnostics.Contracts;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Eum.Spotify.connectstate;
using Wavee.Spotify.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.Sys.IO;

public static class Http<RT>
    where RT : struct, HasCancel<RT>, HasHttp<RT>
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, HttpResponseMessage> Put(string url, HttpContent content, Option<HashMap<string, string>> headers, Option<AuthenticationHeaderValue> auth) =>
        from ct in cancelToken<RT>()
        from response in default(RT).HttpEff.MapAsync(r => r.PutAsync(url, content, headers, auth, ct))
        select response;
}
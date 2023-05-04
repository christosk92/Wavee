using System.Diagnostics.Contracts;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Sys.IO;

public static class Http<RT> where RT : struct, HasHttp<RT>
{
    /// <summary>
    /// Perform a GET request to the remote host
    /// </summary>
    /// <param name="url">
    /// The url to GET
    /// </param>
    /// <param name="auth">
    /// The authentication header to use if any
    /// </param>
    /// <param name="headers">
    /// The headers to send if any
    /// </param>
    /// <typeparam name="RT">Runtime</typeparam>
    /// <returns>The http response message.</returns>
    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, HttpResponseMessage> Get(string url, Option<AuthenticationHeaderValue> auth,
        Option<HashMap<string, string>> headers) =>
        from ct in cancelToken<RT>()
        from httpResponse in default(RT).HttpEff.MapAsync(e => e.Get(url, auth, headers, ct))
        select httpResponse;
}
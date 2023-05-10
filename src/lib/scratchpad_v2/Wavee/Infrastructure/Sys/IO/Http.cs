using System.Diagnostics.Contracts;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using LanguageExt.Common;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Sys.IO;

public static class Http<RT> where RT : struct, HasHttp<RT>
{
    /// <summary>
    /// Perform a GET request to the remote host
    /// </summary>
    /// <param name="url">
    ///     The url to GET
    /// </param>
    /// <param name="auth">
    ///     The authentication header to use if any
    /// </param>
    /// <param name="headers">
    ///     The headers to send if any
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="RT">Runtime</typeparam>
    /// <returns>The http response message.</returns>
    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, HttpResponseMessage> Get(string url, Option<AuthenticationHeaderValue> auth,
        Option<HashMap<string, string>> headers, CancellationToken cancellationToken = default) =>
        from httpResponse in default(RT).HttpEff.MapAsync(e => e.Get(url, auth, headers, cancellationToken))
        select httpResponse;

    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, HttpResponseMessage> GetWithContentRange(string url, int start, int length,
        CancellationToken ct = default)
        => from httpResponse in default(RT).HttpEff.MapAsync(e => e.GetWithContentRange(url, start, length, ct))
            select httpResponse;

    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, HttpResponseMessage> Put(string url,
        Option<AuthenticationHeaderValue> authheader,
        Option<HashMap<string, string>> headers,
        HttpContent content,
        CancellationToken ct = default) =>
        from httpResponse in default(RT).HttpEff.MapAsync(e => e.Put(url, authheader, headers, content, ct))
        select httpResponse;
}
using System.Net.Http.Headers;

namespace Wavee.Spotify.Infrastructure.Traits;

public interface HttpIO
{
    ValueTask<HttpResponseMessage> PutAsync(
        string url,
        HttpContent content,
        Option<HashMap<string, string>> headers,
        Option<AuthenticationHeaderValue> auth,
        CancellationToken ct = default);
}

/// <summary>
/// Type-class giving a struct the trait of supporting HTTP IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasHttp<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the HTTP synchronous effect environment
    /// </summary>
    /// <returns>HTTP synchronous effect environment</returns>
    Eff<RT, HttpIO> HttpEff { get; }
}
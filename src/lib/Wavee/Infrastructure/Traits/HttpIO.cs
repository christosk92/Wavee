using System.Net.Http.Headers;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.Infrastructure.Traits;

public interface HttpIO
{
    ValueTask<HttpResponseMessage> Get(string url, Option<AuthenticationHeaderValue> authentication,
        Option<HashMap<string, string>> headers, CancellationToken ct = default);

    ValueTask<HttpResponseMessage> GetWithContentRange(string url, int start, int length,
        CancellationToken ct = default);

    ValueTask<HttpResponseMessage> Put(string url, Option<AuthenticationHeaderValue> authheader,
        Option<HashMap<string, string>> headers, HttpContent content, CancellationToken ct = default);
}

/// <summary>
/// Type-class giving a struct the trait of supporting Http IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasHttp<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the Http synchronous effect environment
    /// </summary>
    /// <returns>Http synchronous effect environment</returns>
    Eff<RT, HttpIO> HttpEff { get; }
}
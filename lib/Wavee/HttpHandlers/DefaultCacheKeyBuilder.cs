using Wavee.Interfaces;

namespace Wavee.HttpHandlers;

/// <summary>
/// Builds cache keys based on the request URI and Accept-Language header.
/// </summary>
public class DefaultCacheKeyBuilder : ICacheKeyBuilder
{
    /// <inheritdoc />
    public string BuildCacheKey(HttpRequestMessage request)
    {
        var uri = request.RequestUri?.ToString() ?? string.Empty;
        var language = "en"; // Default language

        if (request.Headers.AcceptLanguage.Any())
        {
            language = string.Join(",", request.Headers.AcceptLanguage.Select(al => al.Value));
        }

        return $"{uri}|{language}";
    }
}

namespace Wavee.Interfaces;

/// <summary>
/// Defines a contract for building cache keys from HTTP requests.
/// </summary>
public interface ICacheKeyBuilder
{
    /// <summary>
    /// Builds a unique cache key based on the HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <returns>A unique string representing the cache key.</returns>
    string BuildCacheKey(HttpRequestMessage request);
}
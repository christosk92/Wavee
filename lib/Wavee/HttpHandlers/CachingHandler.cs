// using Microsoft.Extensions.Logging;
// using Wavee.Interfaces;
// using Wavee.Repositories;
//
// namespace Wavee.HttpHandlers;
//
// /// <summary>
// /// A DelegatingHandler that implements caching logic using ETags, Content-Type, and a cache repository.
// /// </summary>
// public class CachingHandler : DelegatingHandler
// {
//     private readonly ILogger<CachingHandler> _logger;
//     private readonly ICacheRepository<string> _cacheRepository;
//     private readonly ICacheKeyBuilder _cacheKeyBuilder;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="CachingHandler"/> class.
//     /// </summary>
//     /// <param name="cacheRepository">The cache repository for storing and retrieving cached data.</param>
//     /// <param name="cacheKeyBuilder">The cache key builder for generating unique cache keys.</param>
//     /// <param name="logger">The logger instance for logging operations.</param>
//     public CachingHandler(
//         ICacheRepository<string> cacheRepository,
//         ICacheKeyBuilder cacheKeyBuilder,
//         ILogger<CachingHandler> logger)
//     {
//         _cacheRepository = cacheRepository ?? throw new ArgumentNullException(nameof(cacheRepository));
//         _cacheKeyBuilder = cacheKeyBuilder ?? throw new ArgumentNullException(nameof(cacheKeyBuilder));
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     }
//
//     /// <inheritdoc />
//     protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
//         CancellationToken cancellationToken)
//     {
//         // Only cache GET requests
//         if (request.Method != HttpMethod.Get)
//         {
//             _logger.LogDebug($"Non-GET request detected. Passing through without caching.");
//             return await base.SendAsync(request, cancellationToken);
//         }
//
//         var cacheKey = _cacheKeyBuilder.BuildCacheKey(request);
//         _logger.LogDebug($"Processing cache for key: {cacheKey}");
//
//         // Retrieve ETag from cache
//         var cachedItem = await _cacheRepository.GetAsync(cacheKey);
//         if (cachedItem?.ETag != null)
//         {
//             _logger.LogDebug($"Adding If-None-Match header with ETag: {cachedItem.ETag} for key: {cacheKey}");
//             request.Headers.IfNoneMatch.ParseAdd(cachedItem.ETag);
//         }
//
//         // Send the request
//         var response = await base.SendAsync(request, cancellationToken);
//
//         if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
//         {
//             _logger.LogInformation($"Cache hit (Not Modified) for key: {cacheKey}");
//             if (cachedItem != null)
//             {
//                 // Create a new response with cached data
//                 var cachedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
//                 {
//                     Content = new ByteArrayContent(cachedItem.Data),
//                     RequestMessage = request,
//                     ReasonPhrase = "OK (from cache)",
//                     Version = response.Version
//                 };
//
//                 // Set the Content-Type header
//                 if (!string.IsNullOrEmpty(cachedItem.ContentType))
//                 {
//                     cachedResponse.Content.Headers.ContentType =
//                         new System.Net.Http.Headers.MediaTypeHeaderValue(cachedItem.ContentType);
//                     _logger.LogDebug($"Set Content-Type to: {cachedItem.ContentType} for key: {cacheKey}");
//                 }
//
//                 // Optionally, copy other headers if necessary
//                 foreach (var header in response.Headers)
//                 {
//                     cachedResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
//                 }
//
//                 return cachedResponse;
//             }
//         }
//         else if (response.IsSuccessStatusCode)
//         {
//             // Read and cache the response data
//             var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
//             var etag = response.Headers.ETag?.Tag;
//             var contentType = response.Content.Headers.ContentType?.MediaType;
//
//             var cacheItem = new CacheItem
//             {
//                 ETag = etag,
//                 ContentType = contentType,
//                 Data = responseBytes
//             };
//
//             await _cacheRepository.SetAsync(cacheKey, cacheItem);
//
//             _logger.LogInformation($"Cached response for key: {cacheKey}");
//
//             return response;
//         }
//         else
//         {
//             _logger.LogWarning($"Received non-success status code: {response.StatusCode} for key: {cacheKey}");
//         }
//
//         return response;
//     }
// }
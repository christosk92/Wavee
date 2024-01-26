using System.Collections.Immutable;
using Google.Protobuf;
using LanguageExt;
using Spotify.Collection.Proto.V2;
using TagLib.Ogg;

namespace Wavee.Spfy;

public sealed class WaveeSpotifyLibraryClient
{
    private readonly IHttpClient _httpClient;
    private readonly Func<ValueTask<(string Username, string Token)>> _tokenFactory;
    private readonly Option<ICachingProvider> _cachingProvider;

    internal WaveeSpotifyLibraryClient(
        IHttpClient httpClient,
        Func<ValueTask<(string Username, string Token)>> tokenFactory,
        Option<ICachingProvider> cachingProvider)
    {
        _httpClient = httpClient;
        _tokenFactory = tokenFactory;
        _cachingProvider = cachingProvider;
    }


    public IAsyncEnumerable<WaveeSpotifyLibraryPageResponse> GetArtists() => Get("artist");

    public IAsyncEnumerable<WaveeSpotifyLibraryPageResponse> GetCollection() => Get("collection");

    private async IAsyncEnumerable<WaveeSpotifyLibraryPageResponse> Get(string set)
    {
        var hasNextPage = true;
        string? nextToken = null;
        while (hasNextPage)
        {
            //https://gae2-spclient.spotify.com/collection/v2/paging
            var ap = await ApResolve.GetSpClient(_httpClient);
            var url = $"https://{ap}/collection/v2/paging";
            var (user, token) = await _tokenFactory();
            var request = new PageRequest
            {
                Username = user,
                Limit = 1000,
                Set = set
            };
            if (!string.IsNullOrEmpty(nextToken))
            {
                request.PaginationToken = nextToken;
            }

            const string contentType = "application/vnd.collection-v2.spotify.proto";
            using var response = await _httpClient.Post(url, token, request.ToByteArray(), contentType);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                var pagedResponse = PageResponse.Parser.ParseFrom(stream);
                nextToken = pagedResponse.NextPageToken;
                hasNextPage = !string.IsNullOrEmpty(nextToken);
                yield return CreateFrom(pagedResponse);
            }
            else
            {
                hasNextPage = false;
            }

        }
    }

    private WaveeSpotifyLibraryPageResponse CreateFrom(PageResponse pagedResponse)
    {
        var l = pagedResponse.Items.Select(x => new WaveeSpotifyLibraryItem(Id:
            SpotifyId.FromUri(x.Uri), AddedAt: DateTimeOffset.FromUnixTimeSeconds(x.AddedAt)));
        return new WaveeSpotifyLibraryPageResponse(l.ToImmutableArray());
    }
}

public readonly record struct WaveeSpotifyLibraryItem(SpotifyId Id, DateTimeOffset AddedAt);

public sealed class WaveeSpotifyLibraryPageResponse
{
    public WaveeSpotifyLibraryPageResponse(IReadOnlyCollection<WaveeSpotifyLibraryItem> items)
    {
        Items = items;
    }

    public IReadOnlyCollection<WaveeSpotifyLibraryItem> Items { get; }
}
